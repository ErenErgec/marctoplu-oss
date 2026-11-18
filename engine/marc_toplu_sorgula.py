"""
marc_toplu_sorgula.py — MARC Toplu Sorgulama Motoru

Bu modül, job_runner.py tarafından çağrılır.
run_batch() fonksiyonu zorunludur.

İmza:
    run_batch(
        input_path: str,
        output_path: str,
        progress_callback: Callable[[int, int, str, dict|None, str], None] | None,
        cancel_check: Callable[[], bool] | None,
    ) -> tuple[list[dict], str | None]

Dönüş:
    (results, output_file_path)
    - results: İşlenen her ISBN için dict listesi
    - output_file_path: Çıktı Excel dosyasının yolu (None ise hata)

Kaynak öncelik sırası:
    1. LOC SRU (Library of Congress) — gerçek MARC21 XML, en yüksek doğruluk
    2. TO-KAT (Ulusal Toplu Katalog) — Web scraping ile Türkiye kütüphaneleri (Z3988 COinS)
    3. Open Library — geniş kapsam, Türkçe yayınlar dahil
    4. Google Books  — son çare fallback
    Türk ISBN'lerinde (978-975/605/625/9944) TO-KAT ilk sırada sorgulanır.

ISBN önbelleği:
    run_batch(cache_path=...) verilirse sonuçlar SQLite'ta tutulur (bkz. IsbnCache).
    Önbellekte taze kaydı olan ISBN için dış kaynaklara gidilmez ve bekleme yapılmaz.
"""

import json
import logging
import re
import sqlite3
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

try:
    from openpyxl import Workbook, load_workbook
    from openpyxl.styles import Alignment, Font, PatternFill
except ImportError as e:
    raise ImportError(
        "openpyxl yüklü değil. Lütfen 'pip install openpyxl' komutunu çalıştırın."
    ) from e

log = logging.getLogger(__name__)

# ─────────────────────────────────────────────────────────────
# Sabitler
# ─────────────────────────────────────────────────────────────

_USER_AGENT = "MARCToken-Engine/1.0 (library cataloging; python)"
_HTTP_TIMEOUT = 12          # saniye
_RATE_LIMIT_DELAY = 0.35    # kaynaklar arası bekleme (saniye)

# LOC SRU — gerçek MARC21 XML döndürür, API anahtarı gerekmez
_LOC_SRU_URL = "http://lx2.loc.gov:210/LCDB"

# TO-KAT (Ulusal Toplu Katalog) — Web scraping
_TOKAT_URL = "https://toplukatalog.tr/"

# Open Library — ISBN → kitap metadata, Türkçe yayınları kapsar
_OPEN_LIBRARY_URL = "https://openlibrary.org/api/books"

# Google Books — fallback; rate limit riski var
_GOOGLE_BOOKS_URL = "https://www.googleapis.com/books/v1/volumes"

# MARC21 XML namespace
_MARC_NS = "http://www.loc.gov/MARC21/slim"

# ISBN önbelleği geçerlilik süreleri. Bulunamayanlar kısa tutulur: kaynaklar zamanla kayıt ekleyebilir.
_CACHE_TTL_FOUND = 180 * 24 * 3600      # 180 gün
_CACHE_TTL_NOT_FOUND = 14 * 24 * 3600   # 14 gün


# ─────────────────────────────────────────────────────────────
# HTTP yardımcısı
# ─────────────────────────────────────────────────────────────

def _http_get(url: str, params: dict | None = None) -> bytes:
    """
    Basit HTTP GET isteği.
    503/429 durumunda kısa bir backoff ile 1 kez yeniden dener.
    """
    if params:
        url = f"{url}?{urllib.parse.urlencode(params)}"

    req = urllib.request.Request(url, headers={"User-Agent": _USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=_HTTP_TIMEOUT) as resp:
            return resp.read()
    except urllib.error.HTTPError as e:
        if e.code in (429, 503):
            log.warning("Rate-limited (%s), 3 saniye bekleniyor: %s", e.code, url)
            time.sleep(3)
            with urllib.request.urlopen(req, timeout=_HTTP_TIMEOUT) as resp:
                return resp.read()
        raise


def _http_get_json(url: str, params: dict | None = None) -> dict:
    raw = _http_get(url, params)
    return json.loads(raw.decode("utf-8"))


def _http_get_xml(url: str, params: dict | None = None) -> ET.Element:
    raw = _http_get(url, params)
    return ET.fromstring(raw.decode("utf-8"))


# ─────────────────────────────────────────────────────────────
# ISBN araçları
# ─────────────────────────────────────────────────────────────

def _clean_isbn(isbn: str) -> str:
    """Tire, boşluk ve geçersiz karakterleri kaldırır."""
    return re.sub(r"[^0-9Xx]", "", isbn).upper()


def _isbn10_to_isbn13(isbn10: str) -> str:
    """10 haneli ISBN'yi 13 haneye çevirir (ISBN-13 = 978 + ilk 9 hane + check)."""
    if len(isbn10) != 10:
        return isbn10
    digits = "978" + isbn10[:9]
    total = sum(int(d) * (1 if i % 2 == 0 else 3) for i, d in enumerate(digits))
    check = (10 - (total % 10)) % 10
    return digits + str(check)


def _isbn13_to_isbn10(isbn13: str) -> str:
    """13 haneli ISBN'yi 10 haneye çevirir (sadece 978 prefix için)."""
    if not isbn13.startswith("978") or len(isbn13) != 13:
        return ""
    core = isbn13[3:12]
    total = sum(int(d) * (10 - i) for i, d in enumerate(core))
    check = (11 - (total % 11)) % 11
    return core + ("X" if check == 10 else str(check))


def _normalize_isbn(raw: str) -> tuple[str, str]:
    """
    Girdi ISBN'yi normalize eder.
    Dönüş: (isbn13, isbn10)  — biri veya ikisi boş olabilir.
    """
    clean = _clean_isbn(raw)
    if len(clean) == 13:
        return clean, _isbn13_to_isbn10(clean)
    if len(clean) == 10:
        return _isbn10_to_isbn13(clean), clean
    # Geçersiz uzunluk — ham değeri döndür
    return clean, ""


# ─────────────────────────────────────────────────────────────
# MARC alan montajı
# ─────────────────────────────────────────────────────────────

def _build_marc_020(isbn: str) -> str:
    """MARC 020 $a ISBN"""
    return f"$a {isbn}"


def _build_marc_100(author_name: str) -> str:
    """MARC 100 $a ad, soyad, $e ilişki terimi"""
    if not author_name:
        return ""
    name = author_name.strip().rstrip(",")
    return f"$a {name}, $e yazar."


def _build_marc_245(title: str, subtitle: str = "", statement: str = "") -> str:
    """MARC 245 $a başlık $b alt başlık $c sorumluluk"""
    parts = [f"$a {title.strip()}"]
    if subtitle:
        parts.append(f"$b {subtitle.strip()}")
    if statement:
        parts.append(f"$c {statement.strip()}")
    return " ".join(parts)


def _build_marc_260(place: str = "", publisher: str = "", year: str = "") -> str:
    """MARC 260 $a yer : $b yayınevi, $c yıl."""
    parts = []
    if place:
        parts.append(f"$a {place.strip()} :")
    if publisher:
        parts.append(f"$b {publisher.strip()},")
    if year:
        parts.append(f"$c {year.strip()}.")
    return " ".join(parts)


def _extract_year(text: str) -> str:
    """Metin içinden 4 haneli yıl çıkarır."""
    m = re.search(r"\b(1[5-9]\d{2}|20\d{2})\b", text)
    return m.group(1) if m else ""


# ─────────────────────────────────────────────────────────────
# Kaynak 1: Library of Congress SRU — gerçek MARC21 XML
# ─────────────────────────────────────────────────────────────

def _parse_marc_datafield(record: ET.Element, tag: str) -> list[dict[str, str]]:
    """
    MARC21 XML'den belirtilen alan etiketine ait tüm datafield'ları döndürür.
    Her biri {subfield_code: value} şeklinde dict.
    """
    fields = []
    for df in record.findall(f"{{{_MARC_NS}}}datafield[@tag='{tag}']"):
        sf_map: dict[str, list[str]] = {}
        for sf in df.findall(f"{{{_MARC_NS}}}subfield"):
            code = sf.get("code", "")
            sf_map.setdefault(code, []).append(sf.text or "")
        fields.append({k: " ".join(v) for k, v in sf_map.items()})
    return fields


def _query_loc_sru(isbn: str) -> dict | None:
    """
    Library of Congress SRU ile MARC21 XML kaydı çeker.
    Kaynak: http://lx2.loc.gov:210/LCDB

    Avantaj: Gerçek, kataloglanmış MARC alanları.
    Kapsam : Ağırlıklı olarak İngilizce yayınlar; Türkçe yayınlar sınırlı.
    """
    params = {
        "operation": "searchRetrieve",
        "version": "1.1",
        "query": f"bath.isbn={isbn}",
        "recordSchema": "marcxml",
        "maximumRecords": "1",
    }
    try:
        root = _http_get_xml(_LOC_SRU_URL, params)
    except Exception as e:
        log.debug("LOC SRU hatası: %s", e)
        return None

    # numberOfRecords kontrolü
    ns = "http://www.loc.gov/zing/srw/"
    num_el = root.find(f"{{{ns}}}numberOfRecords")
    if num_el is None or (num_el.text or "").strip() == "0":
        return None

    # İlk kaydı al
    record_data = root.find(f".//{{{ns}}}recordData")
    if record_data is None:
        return None
    record = record_data.find(f"{{{_MARC_NS}}}record")
    if record is None:
        return None

    def sf(tag: str, code: str) -> str:
        """İlk eşleşen datafield'ın belirtilen subfield'ını döndürür."""
        fields = _parse_marc_datafield(record, tag)
        return fields[0].get(code, "") if fields else ""

    def sf_all(tag: str, code: str) -> list[str]:
        """Tüm eşleşen subfield değerlerini listeler."""
        return [f.get(code, "") for f in _parse_marc_datafield(record, tag) if f.get(code)]

    # ── Alan çıkarımı ─────────────────────────────────────────
    # Başlık (245)
    # ISBD noktalaması ($a "Başlık :", $b "alt başlık /") Excel başlığında tekrarlanmasın
    title_a = sf("245", "a").rstrip(" /:;=").strip()
    title_b = sf("245", "b").rstrip(" /:;=").strip()
    title_c = sf("245", "c").strip()

    # Yazar (100 kişisel ad, 110 kurum, 111 kongre)
    author_a = sf("100", "a").rstrip(",").strip()
    author_d = sf("100", "d").strip()  # tarih

    # Ek yazarlar (700)
    added_authors = sf_all("700", "a")

    # Tüm yazarlar birleşik
    all_authors_list = []
    if author_a:
        all_authors_list.append(author_a)
    for a in added_authors:
        clean_a = a.rstrip(",").strip()
        if clean_a and clean_a not in all_authors_list:
            all_authors_list.append(clean_a)
    all_authors = "; ".join(all_authors_list)

    # Yayın bilgisi — önce 264, yoksa 260
    place   = sf("264", "a") or sf("260", "a")
    pub     = sf("264", "b") or sf("260", "b")
    year_c  = sf("264", "c") or sf("260", "c")
    year    = _extract_year(year_c)

    place   = place.rstrip(" :").strip()
    pub     = pub.rstrip(",").strip()

    # Dil (008 control field sabit konumu 35-37)
    lang = ""
    cf008 = record.findtext(f"{{{_MARC_NS}}}controlfield[@tag='008']") or ""
    if len(cf008) >= 38:
        lang = cf008[35:38].strip()

    # ISBN (020 $a)
    # Sorgulanan ISBN'in dönen kayıtta gerçekten bulunduğunu doğrula.
    # LOC bazen farklı ISBN'e ait kaydı döndürebilir.
    returned_isbns = sf_all("020", "a")
    clean_query = _normalize_isbn(isbn)[0]
    if returned_isbns:
        # 020 $a "0-306-40615-2 (pbk.)" gibi tire ve ek içerebilir: ilk kelimeyi al, tireleri at.
        # ISBN-10 ve ISBN-13 aynı kitap sayılır.
        clean_returned = [
            _normalize_isbn(re.sub(r"[^0-9Xx]", "", re.split(r"[\s(:;]", i.strip())[0]))[0]
            for i in returned_isbns
        ]
        if clean_query not in clean_returned:
            log.debug(
                "LOC ISBN uyuşmazlığı: sorgu=%s, dönen=%s",
                clean_query, clean_returned,
            )
            return None

    isbn_020 = returned_isbns[0] if returned_isbns else isbn

    # Konu başlıkları (650 $a)
    subjects = sf_all("650", "a")

    # ── MARC alan stringleri ───────────────────────────────────
    marc_020 = _build_marc_020(isbn_020)
    marc_100 = _build_marc_100(author_a)
    marc_245 = _build_marc_245(title_a, title_b, title_c)
    marc_260 = _build_marc_260(place, pub, year)

    return {
        "isbn": isbn,
        "title": f"{title_a}{': ' + title_b if title_b else ''}",
        "author": all_authors,
        "publisher": pub,
        "year": year,
        "language": lang,
        "marc_020": marc_020,
        "marc_100": marc_100,
        "marc_245": marc_245,
        "marc_260": marc_260,
        "subjects": "; ".join(subjects),
        "status": "found",
        "source": "LOC",
        "error_message": None,
        # Kataloğa birebir aktarım için LoC'un tam kaydı (MARCXML/.mrc çıktısında kullanılır)
        "marc_xml": ET.tostring(record, encoding="unicode"),
    }


# ─────────────────────────────────────────────────────────────
# Kaynak 2: Open Library Books API
# ─────────────────────────────────────────────────────────────

def _query_open_library(isbn: str) -> dict | None:
    """
    Open Library Books API ile kitap kaydı çeker.
    Kaynak: https://openlibrary.org/api/books

    Avantaj: Geniş kapsam; Türkçe yayınlar dahil pek çok ISBN.
    Not   : MARC alanları doğrudan gelmez, metadata'dan sentezlenir.
    """
    isbn_key = f"ISBN:{isbn}"
    try:
        data = _http_get_json(_OPEN_LIBRARY_URL, {
            "bibkeys": isbn_key,
            "format": "json",
            "jscmd": "data",
        })
    except Exception as e:
        log.debug("Open Library hatası: %s", e)
        return None

    book = data.get(isbn_key)
    if not book:
        return None

    # ── Metadata çıkarımı ─────────────────────────────────────
    title = book.get("title", "")
    subtitle = book.get("subtitle", "")

    authors_raw = book.get("authors", [])
    author_names = [a.get("name", "").strip() for a in authors_raw if a.get("name")]

    publishers_raw = book.get("publishers", [])
    publisher = publishers_raw[0].get("name", "").strip() if publishers_raw else ""

    pub_places_raw = book.get("publish_places", [])
    place = pub_places_raw[0].get("name", "").strip() if pub_places_raw else ""

    pub_date = book.get("publish_date", "")
    year = _extract_year(pub_date) or pub_date

    # Dil
    lang_list = [l.get("key", "").split("/")[-1] for l in book.get("languages", [])]
    language = lang_list[0] if lang_list else ""

    # Sayfa sayısı
    pages = book.get("number_of_pages")

    # Konu başlıkları
    subjects_raw = book.get("subjects", [])
    subjects = "; ".join(s.get("name", s) if isinstance(s, dict) else s for s in subjects_raw[:5])

    # ── MARC alan sentezi ──────────────────────────────────────
    marc_020 = _build_marc_020(isbn)
    marc_100 = _build_marc_100(author_names[0]) if author_names else ""
    marc_245 = _build_marc_245(title, subtitle)
    marc_260 = _build_marc_260(place, publisher, year)

    return {
        "isbn": isbn,
        "title": f"{title}{': ' + subtitle if subtitle else ''}",
        "author": "; ".join(author_names),
        "publisher": publisher,
        "year": year,
        "language": language,
        "pages": str(pages) if pages else "",
        "marc_020": marc_020,
        "marc_100": marc_100,
        "marc_245": marc_245,
        "marc_260": marc_260,
        "subjects": subjects,
        "status": "found",
        "source": "Open Library",
        "error_message": None,
    }


# ─────────────────────────────────────────────────────────────
# Kaynak 3: TO-KAT (Ulusal Toplu Katalog) Web Scraper
# ─────────────────────────────────────────────────────────────

def _query_toplu_katalog(isbn: str) -> dict | None:
    """
    Ulusal Toplu Katalog üzerinden basit arama yaparak HTML'den Z3988 COinS
    (ContextObjects in Spans) verilerini çeker.
    """
    url = f"{_TOKAT_URL}?_f=1&cwid=2&keyword={isbn}&tokat_search_field=4&order=0&command=Tara"
    req = urllib.request.Request(url, headers={"User-Agent": _USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=_HTTP_TIMEOUT) as resp:
            html = resp.read().decode("utf-8", errors="ignore")
    except Exception as e:
        log.debug("TO-KAT hatası: %s", e)
        return None

    # Z3988 span'larını bul
    spans = re.findall(r'<span class="Z3988" title="([^"]+)">', html)
    if not spans:
        return None
        
    # İlk sonucu al
    title_str = spans[0].replace("&amp;", "&")
    params = urllib.parse.parse_qs(title_str)
    
    title = params.get("rft.title", [""])[0]
    author = params.get("rft.au", [""])[0]
    year = params.get("rft.date", [""])[0]
    returned_isbn = params.get("rft.isbn", [""])[0]
    
    # Doğrulama
    clean_query = re.sub(r"[^0-9Xx]", "", isbn).upper()
    if returned_isbn:
        clean_returned = re.sub(r"[^0-9Xx]", "", returned_isbn).upper()
        if clean_query != clean_returned:
            log.debug("TO-KAT ISBN uyuşmazlığı: %s != %s", clean_query, clean_returned)
            return None

    marc_020 = _build_marc_020(returned_isbn or isbn)
    marc_100 = _build_marc_100(author)
    marc_245 = _build_marc_245(title)
    marc_260 = _build_marc_260("", "", year)

    return {
        "isbn": isbn,
        "title": title,
        "author": author,
        "publisher": "",
        "year": year,
        "language": "",
        "pages": "",
        "marc_020": marc_020,
        "marc_100": marc_100,
        "marc_245": marc_245,
        "marc_260": marc_260,
        "subjects": "",
        "status": "found",
        "source": "TO-KAT",
        "error_message": None,
    }


# ─────────────────────────────────────────────────────────────
# Kaynak 4: Google Books API (fallback)
# ─────────────────────────────────────────────────────────────

def _query_google_books(isbn: str) -> dict | None:
    """
    Google Books Volumes API ile kitap kaydı çeker.
    Kaynak: https://www.googleapis.com/books/v1/volumes

    Avantaj: Çok geniş kapsam.
    Dikkat : Rate limit riski (429). API anahtarı gerekmez (sınırlı kota).
    """
    try:
        data = _http_get_json(_GOOGLE_BOOKS_URL, {"q": f"isbn:{isbn}"})
    except Exception as e:
        log.debug("Google Books hatası: %s", e)
        return None

    items = data.get("items")
    if not items:
        return None

    info = items[0].get("volumeInfo", {})

    title = info.get("title", "")
    subtitle = info.get("subtitle", "")
    authors = info.get("authors", [])
    publisher = info.get("publisher", "").strip()
    pub_date = info.get("publishedDate", "")
    year = _extract_year(pub_date) or pub_date
    language = info.get("language", "")
    pages = info.get("pageCount")
    categories = info.get("categories", [])
    description = info.get("description", "")

    # ISBN'leri doğrula
    isbn_identifiers = info.get("industryIdentifiers", [])
    canonical_isbn = next(
        (x["identifier"] for x in isbn_identifiers if x.get("type") == "ISBN_13"),
        isbn,
    )

    marc_020 = _build_marc_020(canonical_isbn)
    marc_100 = _build_marc_100(authors[0]) if authors else ""
    marc_245 = _build_marc_245(title, subtitle)
    marc_260 = _build_marc_260("", publisher, year)

    return {
        "isbn": isbn,
        "title": f"{title}{': ' + subtitle if subtitle else ''}",
        "author": "; ".join(authors),
        "publisher": publisher,
        "year": year,
        "language": language,
        "pages": str(pages) if pages else "",
        "marc_020": marc_020,
        "marc_100": marc_100,
        "marc_245": marc_245,
        "marc_260": marc_260,
        "subjects": "; ".join(categories),
        "description": description[:200] if description else "",
        "status": "found",
        "source": "Google Books",
        "error_message": None,
    }


# ─────────────────────────────────────────────────────────────
# Ana sorgu fonksiyonu — iş motorunun giriş noktası
# ─────────────────────────────────────────────────────────────

# Türkiye'ye ayrılmış ISBN grupları (978-975, 978-605, 978-625, 978-9944)
_TURKISH_ISBN_PREFIXES = ("978975", "978605", "978625", "9789944")

# Varsayılan sorgu sırası: (etiket, fonksiyon adı). Türk ISBN'lerinde TO-KAT öne alınır (bkz. _source_order).
# Fonksiyonlar adla tutulur ve çağrı anında çözülür; testlerde mock.patch ile değiştirilebilir.
_SOURCES = (
    ("LOC", "_query_loc_sru"),
    ("TO-KAT", "_query_toplu_katalog"),
    ("Open Library", "_query_open_library"),
    ("Google Books", "_query_google_books"),
)


def _is_turkish_isbn(isbn13: str) -> bool:
    return len(isbn13) == 13 and isbn13.startswith(_TURKISH_ISBN_PREFIXES)


def _source_order(isbn13: str) -> list[tuple[str, str]]:
    """Türk yayınları çoğunlukla LoC'ta yok; TO-KAT'ı önce sormak bir isteği ve beklemeyi kazandırır."""
    sources = list(_SOURCES)
    if _is_turkish_isbn(isbn13):
        sources.sort(key=lambda s: s[0] != "TO-KAT")
    return sources


def _query_marc(isbn_raw: str) -> dict:
    """
    Tek bir ISBN için MARC kaydı sorgular.

    Öncelik sırası (Türk ISBN'lerinde — 975/605/625/9944 — TO-KAT en başa alınır):
        1. LOC SRU  — Gerçek MARC21 XML (en doğru veri)
        2. TO-KAT   — Türkiye Kütüphaneleri (Z3988 Web Scraping)
        3. Open Library — Geniş kapsam (Türkçe dahil)
        4. Google Books — Son çare fallback

    Args:
        isbn_raw: Ham ISBN numarası (10 veya 13 haneli, tireli olabilir)

    Returns:
        dict: {
            isbn, title, author, publisher, year, language, pages,
            marc_020, marc_100, marc_245, marc_260, subjects,
            status, source, error_message
        }
    """
    isbn13, isbn10 = _normalize_isbn(isbn_raw)
    primary_isbn = isbn13 or isbn10 or _clean_isbn(isbn_raw)
    errors: list[str] = []
    failed_sources: list[str] = []

    for attempt, (name, fn_name) in enumerate(_source_order(primary_isbn)):
        if attempt > 0:
            time.sleep(_RATE_LIMIT_DELAY)
        query = globals()[fn_name]
        try:
            result = query(primary_isbn)
            # Open Library kısa formatla da denenir
            if not result and fn_name == "_query_open_library" and isbn10 and isbn10 != primary_isbn:
                result = query(isbn10)
            if result:
                result["isbn"] = isbn_raw  # orijinal girdi korunur
                return result
        except Exception as e:
            errors.append(f"{name}: {e}")
            failed_sources.append(name)
            log.debug("%s başarısız: %s", name, e)

    empty = {
        "isbn": isbn_raw,
        "title": "",
        "author": "",
        "publisher": "",
        "year": "",
        "language": "",
        "pages": "",
        "marc_020": _build_marc_020(primary_isbn),
        "marc_100": "",
        "marc_245": "",
        "marc_260": "",
        "subjects": "",
        "source": "",
    }

    # Hiçbir kaynağa ulaşılamadı (ağ kesintisi vb.): "bulunamadı" değil, hata
    if len(failed_sources) == len(_SOURCES):
        return {**empty, "status": "error", "error_message": "Kaynaklara ulaşılamadı: " + "; ".join(errors)}

    record = {**empty, "status": "not_found", "error_message": "Kayıt bulunamadı."}
    if failed_sources:
        # Bazı kaynaklar yanıt vermedi: sonuç kesin değil, önbelleğe alınmaz
        record["error_message"] = "Kayıt bulunamadı (yanıt vermeyen kaynak: " + ", ".join(failed_sources) + ")."
        record["incomplete"] = True
    return record


# ─────────────────────────────────────────────────────────────
# MARC21 çıktısı (MARCXML ve ISO 2709 .mrc)
# ─────────────────────────────────────────────────────────────
#
# LoC'tan gelen kayıtlar olduğu gibi (tam MARCXML) yazılır. Diğer kaynaklar için
# Excel'deki alanlardan minimal bir kayıt kurulur: 001, 005, 008, 020, 100/700,
# 245, 260, 300, 650 ve kaynağı belirten 500 notu. Bulunamayan/hatalı ISBN'ler
# MARC çıktısına eklenmez (RIS ile aynı davranış).

_MARC_LANG_2_TO_3 = {
    "tr": "tur", "en": "eng", "de": "ger", "fr": "fre", "es": "spa", "it": "ita",
    "ru": "rus", "ar": "ara", "fa": "per", "ja": "jpn", "zh": "chi", "nl": "dut",
    "pt": "por", "el": "gre", "ku": "kur", "az": "aze", "la": "lat",
}


class MarcRecord:
    """Basit MARC21 kaydı: leader, kontrol alanları (00X) ve veri alanları."""

    def __init__(self, leader: str):
        self.leader = leader
        self.control: list[tuple[str, str]] = []
        self.data: list[tuple[str, str, str, list[tuple[str, str]]]] = []

    def add_control(self, tag: str, value: str) -> None:
        self.control.append((tag, value))

    def add_data(self, tag: str, ind1: str, ind2: str, subfields: list[tuple[str, str]]) -> None:
        subfields = [(c, v) for c, v in subfields if v]
        if subfields:
            self.data.append((tag, ind1 or " ", ind2 or " ", subfields))

    def fields_sorted(self):
        """Kontrol alanları önce, ardından veri alanları etiket sırasıyla (kararlı)."""
        ctrl = sorted(self.control, key=lambda f: f[0])
        data = sorted(self.data, key=lambda f: f[0])
        return ctrl, data


def _marc_from_xml(xml: str) -> MarcRecord | None:
    """LoC MARCXML kaydını MarcRecord'a çevirir."""
    try:
        el = ET.fromstring(xml)
    except ET.ParseError:
        return None
    rec = MarcRecord((el.findtext(f"{{{_MARC_NS}}}leader") or "").ljust(24)[:24])
    for cf in el.findall(f"{{{_MARC_NS}}}controlfield"):
        rec.add_control(cf.get("tag", ""), cf.text or "")
    for df in el.findall(f"{{{_MARC_NS}}}datafield"):
        subs = [(sf.get("code", ""), sf.text or "") for sf in df.findall(f"{{{_MARC_NS}}}subfield")]
        rec.add_data(df.get("tag", ""), df.get("ind1", " "), df.get("ind2", " "), subs)
    return rec


def _parse_subfield_string(value: str) -> list[tuple[str, str]]:
    """Motorun '$a Başlık $b Alt başlık' biçimindeki alan metnini alt alanlara ayırır."""
    if not value:
        return []
    parts = re.split(r"\$([a-z0-9])\s", " " + value.strip())
    return [(parts[i], parts[i + 1].strip()) for i in range(1, len(parts) - 1, 2) if parts[i + 1].strip()]


def _marc_lang(code: str) -> str:
    code = (code or "").strip().lower()
    if len(code) == 3 and code.isalpha():
        return code
    return _MARC_LANG_2_TO_3.get(code[:2], "und")


def _build_marc_record(result: dict, now: float | None = None) -> MarcRecord | None:
    """Bir sonuç satırından MARC kaydı üretir. Bulunamayan/hatalı satırlar için None."""
    if result.get("status") != "found":
        return None
    if result.get("marc_xml"):
        rec = _marc_from_xml(result["marc_xml"])
        if rec is not None:
            return rec

    ts = time.gmtime(now if now is not None else time.time())
    isbn13, isbn10 = _normalize_isbn(result.get("isbn", ""))
    year = _extract_year(str(result.get("year") or ""))

    # Leader: yeni (n), dil materyali (a), monografi (m), UTF-8 (a), kısaltılmış düzey (3), ISBD belirsiz (u)
    rec = MarcRecord("00000nam a22000003u 4500")
    rec.add_control("001", isbn13 or isbn10 or _clean_isbn(result.get("isbn", "")))
    rec.add_control("005", time.strftime("%Y%m%d%H%M%S", ts) + ".0")
    f008 = (
        time.strftime("%y%m%d", ts)          # 00-05 kayıt tarihi
        + ("s" if year else "n")             # 06 tarih türü
        + (year or "    ")                   # 07-10 tarih 1
        + "    "                             # 11-14 tarih 2
        + "xx "                              # 15-17 yayın yeri (bilinmiyor)
        + "|" * 17                           # 18-34 kitaplara özgü kodlanmamış
        + _marc_lang(result.get("language", ""))  # 35-37 dil
        + " d"                               # 38 değişiklik yok, 39 katalog kaynağı: diğer
    )
    rec.add_control("008", f008)

    rec.add_data("020", " ", " ", _parse_subfield_string(result.get("marc_020", "")) or [("a", isbn13)])

    authors = [a.strip() for a in str(result.get("author") or "").split(";") if a.strip()]
    main_entry = _parse_subfield_string(result.get("marc_100", ""))
    if main_entry:
        rec.add_data("100", "1", " ", main_entry)
    elif authors:
        rec.add_data("100", "1", " ", [("a", authors[0])])
    for extra in authors[1:]:
        rec.add_data("700", "1", " ", [("a", extra)])

    title_subs = _parse_subfield_string(result.get("marc_245", "")) or [("a", str(result.get("title") or ""))]
    rec.add_data("245", "1" if (main_entry or authors) else "0", "0", title_subs)

    rec.add_data("260", " ", " ", _parse_subfield_string(result.get("marc_260", ""))
                 or [("b", str(result.get("publisher") or "")), ("c", year)])

    pages = str(result.get("pages") or "").strip()
    if pages:
        rec.add_data("300", " ", " ", [("a", pages if not pages.isdigit() else f"{pages} s.")])

    source = result.get("source") or "bilinmeyen kaynak"
    rec.add_data("500", " ", " ", [("a", f"Kayıt {source} verisinden MARCTOPLU ile otomatik oluşturulmuştur.")])

    for subject in str(result.get("subjects") or "").split(";"):
        if subject.strip():
            rec.add_data("650", " ", "4", [("a", subject.strip())])
    return rec


def _marc_to_iso2709(rec: MarcRecord) -> bytes:
    """MarcRecord'u ISO 2709 (MARC21 iletim biçimi, UTF-8) baytlarına çevirir."""
    FT, SD, RT = b"\x1e", b"\x1f", b"\x1d"
    ctrl, data = rec.fields_sorted()
    directory = b""
    body = b""
    for tag, value in ctrl:
        chunk = value.encode("utf-8") + FT
        directory += tag.encode("ascii") + b"%04d%05d" % (len(chunk), len(body))
        body += chunk
    for tag, ind1, ind2, subs in data:
        chunk = (ind1 + ind2).encode("utf-8")
        for code, val in subs:
            chunk += SD + code.encode("utf-8") + val.encode("utf-8")
        chunk += FT
        directory += tag.encode("ascii") + b"%04d%05d" % (len(chunk), len(body))
        body += chunk
    directory += FT
    base = 24 + len(directory)
    total = base + len(body) + 1
    if total > 99999:
        raise ValueError("MARC kaydı ISO 2709 sınırını (99999 bayt) aşıyor")
    leader = rec.leader.ljust(24)[:24]
    # 00-04 kayıt uzunluğu, 09 'a' (UTF-8), 10-11 '22', 12-16 veri başlangıcı, 20-23 '4500'
    leader = f"{total:05d}" + leader[5:9] + "a22" + f"{base:05d}" + leader[17:20] + "4500"
    return leader.encode("ascii") + directory + body + RT


def _marc_to_xml_element(rec: MarcRecord) -> ET.Element:
    el = ET.Element(f"{{{_MARC_NS}}}record")
    ET.SubElement(el, f"{{{_MARC_NS}}}leader").text = rec.leader
    ctrl, data = rec.fields_sorted()
    for tag, value in ctrl:
        ET.SubElement(el, f"{{{_MARC_NS}}}controlfield", tag=tag).text = value
    for tag, ind1, ind2, subs in data:
        df = ET.SubElement(el, f"{{{_MARC_NS}}}datafield", tag=tag, ind1=ind1, ind2=ind2)
        for code, val in subs:
            ET.SubElement(df, f"{{{_MARC_NS}}}subfield", code=code).text = val
    return el


def _write_output_marc(mrc_path: str, xml_path: str, results: list[dict]) -> int:
    """Bulunan kayıtları .mrc (ISO 2709) ve .xml (MARCXML collection) olarak yazar. Yazılan kayıt sayısını döndürür."""
    records = []
    for r in results:
        try:
            rec = _build_marc_record(r)
        except Exception as exc:  # tek bir bozuk kayıt tüm çıktıyı düşürmesin
            log.warning("ISBN=%s için MARC kaydı oluşturulamadı: %s", r.get("isbn"), exc)
            continue
        if rec is not None:
            records.append((r, rec))

    with open(mrc_path, "wb") as fh:
        for r, rec in records:
            try:
                fh.write(_marc_to_iso2709(rec))
            except ValueError as exc:
                log.warning("ISBN=%s .mrc'ye yazılamadı: %s", r.get("isbn"), exc)

    ET.register_namespace("", _MARC_NS)
    collection = ET.Element(f"{{{_MARC_NS}}}collection")
    for _, rec in records:
        collection.append(_marc_to_xml_element(rec))
    ET.ElementTree(collection).write(xml_path, encoding="utf-8", xml_declaration=True)
    return len(records)


# ─────────────────────────────────────────────────────────────
# ISBN önbelleği
# ─────────────────────────────────────────────────────────────

def _cache_key(isbn_raw: str) -> str:
    """ISBN-10 ve ISBN-13 yazımları aynı anahtara düşer."""
    isbn13, isbn10 = _normalize_isbn(isbn_raw)
    return isbn13 or isbn10 or _clean_isbn(isbn_raw)


class IsbnCache:
    """
    SQLite tabanlı sorgu sonucu önbelleği.

    - Yalnızca "found" ve "not_found" sonuçları saklanır; "error" (ağ/zaman aşımı) saklanmaz.
    - Aynı dosyayı paralel işler kullanabildiği için WAL modu ve busy_timeout açıktır.
    - Önbellek hataları işi asla durdurmaz: okunamazsa/yazılamazsa normal sorguya devam edilir.
    """

    def __init__(self, path: str, now=time.time):
        self._now = now
        self._conn: sqlite3.Connection | None = None
        self.hits = 0
        self.misses = 0
        try:
            Path(path).parent.mkdir(parents=True, exist_ok=True)
            conn = sqlite3.connect(path, timeout=5)
            conn.execute("PRAGMA journal_mode=WAL")
            conn.execute("PRAGMA busy_timeout=5000")
            conn.execute(
                """CREATE TABLE IF NOT EXISTS isbn_cache (
                    isbn TEXT PRIMARY KEY,
                    status TEXT NOT NULL,
                    source TEXT NOT NULL DEFAULT '',
                    record_json TEXT NOT NULL,
                    fetched_at INTEGER NOT NULL
                )"""
            )
            conn.commit()
            self._conn = conn
        except Exception as exc:
            log.warning("ISBN önbelleği açılamadı (%s), önbelleksiz devam ediliyor: %s", path, exc)

    @property
    def enabled(self) -> bool:
        return self._conn is not None

    def get(self, isbn_raw: str) -> dict | None:
        if not self._conn:
            return None
        try:
            row = self._conn.execute(
                "SELECT status, record_json, fetched_at FROM isbn_cache WHERE isbn = ?",
                (_cache_key(isbn_raw),),
            ).fetchone()
        except Exception as exc:
            log.warning("ISBN önbelleği okunamadı: %s", exc)
            return None
        if row is None:
            self.misses += 1
            return None
        status, record_json, fetched_at = row
        ttl = _CACHE_TTL_FOUND if status == "found" else _CACHE_TTL_NOT_FOUND
        if self._now() - fetched_at > ttl:
            self.misses += 1
            return None
        try:
            record = json.loads(record_json)
        except ValueError:
            self.misses += 1
            return None
        record["isbn"] = isbn_raw  # kullanıcının girdiği yazım korunur
        self.hits += 1
        return record

    def put(self, isbn_raw: str, record: dict) -> None:
        if not self._conn or record.get("status") not in ("found", "not_found") or record.get("incomplete"):
            return
        try:
            self._conn.execute(
                "INSERT OR REPLACE INTO isbn_cache (isbn, status, source, record_json, fetched_at) VALUES (?, ?, ?, ?, ?)",
                (
                    _cache_key(isbn_raw),
                    record["status"],
                    record.get("source") or "",
                    json.dumps(record, ensure_ascii=False),
                    int(self._now()),
                ),
            )
            self._conn.commit()
        except Exception as exc:
            log.warning("ISBN önbelleğine yazılamadı: %s", exc)

    def close(self) -> None:
        if self._conn:
            try:
                self._conn.close()
            except Exception:
                pass
            self._conn = None


# ─────────────────────────────────────────────────────────────
# Excel okuma yardımcısı
# ─────────────────────────────────────────────────────────────

def _read_isbns_from_excel(input_path: str) -> list[str]:
    """
    Excel dosyasından ISBN listesini okur.

    Desteklenen formatlar:
    - Başlık satırı "isbn" (büyük/küçük harf fark etmez) olan sütun
    - Başlık satırı yoksa ilk sütun kullanılır
    - Tekrarlı ISBN'ler tekilleştirilir, sıra korunur
    """
    wb = load_workbook(input_path, read_only=True, data_only=True)
    ws = wb.active
    rows = list(ws.iter_rows(values_only=True))
    wb.close()

    if not rows:
        return []

    # Başlık satırını bul
    isbn_col_idx = 0
    first_row = [str(c).strip() if c is not None else "" for c in rows[0]]
    for i, cell_val in enumerate(first_row):
        if cell_val.lower() == "isbn":
            isbn_col_idx = i
            break

    has_header = any(v.lower() == "isbn" for v in first_row if v)
    start_row = 1 if has_header else 0

    seen: set[str] = set()
    isbns: list[str] = []
    for row in rows[start_row:]:
        if not row or isbn_col_idx >= len(row):
            continue
        raw = str(row[isbn_col_idx]).strip() if row[isbn_col_idx] is not None else ""
        if not raw or raw.lower() == "isbn":
            continue
        # Tekilleştirme için normalize et, ama orijinal değeri sakla
        normalized = re.sub(r"[^0-9Xx]", "", raw).upper()
        if normalized and normalized not in seen:
            seen.add(normalized)
            isbns.append(raw)

    return isbns


# ─────────────────────────────────────────────────────────────
# Excel çıktı yazma
# ─────────────────────────────────────────────────────────────

_HDR_FILL = PatternFill(start_color="1A3C34", end_color="1A3C34", fill_type="solid")
_HDR_FONT = Font(color="FFFFFF", bold=True)
_NOT_FOUND_FILL = PatternFill(start_color="FFF3CD", end_color="FFF3CD", fill_type="solid")
_ERROR_FILL = PatternFill(start_color="F8D7DA", end_color="F8D7DA", fill_type="solid")
_FOUND_FONT = Font(color="155724")


def _write_output_excel(output_path: str, results: list[dict]) -> None:
    """İşleme sonuçlarını renk kodlu Excel dosyasına yazar."""
    wb = Workbook()
    ws = wb.active
    ws.title = "MARC Sonuçları"

    headers = [
        "ISBN",
        "Durum",
        "Başlık",
        "Yazar",
        "Yayınevi",
        "Yıl",
        "Dil",
        "Sayfa",
        "Konu Başlıkları",
        "MARC 100 (Yazar)",
        "MARC 245 (Başlık)",
        "MARC 260 (Yayın)",
        "MARC 020 (ISBN)",
        "Hata / Not",
    ]

    col_widths = [16, 12, 42, 28, 26, 8, 8, 7, 38, 32, 44, 28, 18, 38]

    # Başlık satırı
    for col_idx, (header, width) in enumerate(zip(headers, col_widths), 1):
        cell = ws.cell(row=1, column=col_idx, value=header)
        cell.fill = _HDR_FILL
        cell.font = _HDR_FONT
        cell.alignment = Alignment(horizontal="center", vertical="center", wrap_text=True)
        ws.column_dimensions[cell.column_letter].width = width

    ws.row_dimensions[1].height = 30
    ws.freeze_panes = "A2"

    # Veri satırları
    for row_idx, result in enumerate(results, 2):
        status = result.get("status", "")
        values = [
            result.get("isbn", ""),
            status,
            result.get("title", ""),
            result.get("author", ""),
            result.get("publisher", ""),
            result.get("year", ""),
            result.get("language", ""),
            result.get("pages", ""),
            result.get("subjects", ""),
            result.get("marc_100", ""),
            result.get("marc_245", ""),
            result.get("marc_260", ""),
            result.get("marc_020", ""),
            result.get("error_message", ""),
        ]
        for col_idx, val in enumerate(values, 1):
            cell = ws.cell(row=row_idx, column=col_idx, value=val)
            # Durum bazlı renklendirme
            if status == "not_found":
                cell.fill = _NOT_FOUND_FILL
            elif status == "error":
                cell.fill = _ERROR_FILL

    # Filtre ekle
    ws.auto_filter.ref = ws.dimensions

    wb.save(output_path)


def _write_output_ris(ris_path: str, results: list[dict]) -> tuple[int, int]:
    """
    İşleme sonuçlarından sadece bulunan ('found') kayıtları RIS formatında yazar.
    Dönüş: (found_count, not_found_count)
    """
    found_count = 0
    not_found_count = 0
    lines: list[str] = []

    for r in results:
        status = r.get("status", "")
        if status != "found":
            not_found_count += 1
            continue

        found_count += 1
        lines.append("TY  - BOOK")

        # Başlık
        title = (r.get("title") or "").strip()
        if title:
            lines.append(f"TI  - {title}")

        # Yazar(lar)
        author = (r.get("author") or "").strip()
        if author:
            authors = [a.strip() for a in author.split(";") if a.strip()]
            for a in authors:
                lines.append(f"AU  - {a}")

        # Yayınevi
        publisher = (r.get("publisher") or "").strip()
        if publisher:
            lines.append(f"PB  - {publisher}")

        # Yıl
        year = (r.get("year") or "").strip()
        if year:
            year_match = re.search(r"\b(1\d{3}|20\d{2})\b", year)
            if year_match:
                lines.append(f"PY  - {year_match.group(1)}")
            else:
                lines.append(f"PY  - {year}")

        # ISBN
        isbn = (r.get("isbn") or "").strip()
        if isbn:
            lines.append(f"SN  - {isbn}")

        # Dil
        language = (r.get("language") or "").strip()
        if language:
            lines.append(f"LA  - {language}")

        # Sayfa sayısı
        pages = (r.get("pages") or "").strip()
        if pages:
            lines.append(f"SP  - {pages}")

        # Konu Başlıkları (KW)
        subjects = (r.get("subjects") or "").strip()
        if subjects:
            subj_list = [s.strip() for s in subjects.split(";") if s.strip()]
            for s in subj_list:
                lines.append(f"KW  - {s}")

        # Kayıt Sonu
        lines.append("ER  - \n")

    content = "\n".join(lines)
    Path(ris_path).write_text(content, encoding="utf-8")
    return found_count, not_found_count


# ─────────────────────────────────────────────────────────────
# Ana işlev — job_runner.py tarafından çağrılır
# ─────────────────────────────────────────────────────────────

def run_batch(
    input_path: str,
    output_path: str,
    progress_callback=None,
    cancel_check=None,
    cache_path: str | None = None,
) -> tuple[list[dict], str | None]:
    """
    Excel dosyasındaki ISBN'leri toplu olarak işler.

    Args:
        input_path:        Kaynak Excel dosyasının tam yolu.
        output_path:       Çıktı Excel dosyasının tam yolu.
        progress_callback: İlerleme bildirimi için callback.
                           İmza: (idx, total, isbn, marc_record, message)
        cancel_check:      İptal kontrolü için callback.
                           İmza: () -> bool  (True ise iptal)
        cache_path:        ISBN önbelleği SQLite dosyası. None ise önbellek kullanılmaz.

    Returns:
        (results, output_file_path)
        - results: Her ISBN için sonuç dict listesi
        - output_file_path: Başarıyla oluşturulan dosyanın yolu; hata varsa None
    """
    isbns = _read_isbns_from_excel(input_path)
    total = len(isbns)

    if total == 0:
        raise ValueError("Excel dosyasında geçerli ISBN bulunamadı.")

    log.info("Toplu sorgu başlıyor: %d ISBN", total)
    results: list[dict] = []
    cache = IsbnCache(cache_path) if cache_path else None

    try:
        for idx, isbn in enumerate(isbns):
            if cancel_check and cancel_check():
                log.info("İptal sinyali alındı. %d/%d işlendi.", idx, total)
                break

            record = cache.get(isbn) if cache else None
            from_cache = record is not None
            if not from_cache:
                record = _query_isbn_safe(isbn)
                if cache:
                    cache.put(isbn, record)

            results.append(record)

            if progress_callback:
                found_label = "✓" if record.get("status") == "found" else "✗"
                cache_label = " (önbellek)" if from_cache else ""
                progress_callback(
                    idx + 1,
                    total,
                    isbn,
                    record,
                    f"{found_label} {idx + 1}/{total} — {isbn}{cache_label}",
                )

            # Dış kaynaklara istek atıldıysa rate limiting; önbellekten gelende beklemeye gerek yok
            if not from_cache:
                time.sleep(_RATE_LIMIT_DELAY)
    finally:
        if cache:
            if cache.enabled:
                log.info("ISBN önbelleği: %d isabet, %d kaçırma", cache.hits, cache.misses)
            cache.close()

    if not results:
        return ([], None)

    # Çıktı dosyasını yaz
    try:
        _write_output_excel(output_path, results)
        output_file = output_path if Path(output_path).exists() else None
    except Exception as exc:
        raise RuntimeError(f"Çıktı Excel dosyası yazılamadı: {exc}") from exc

    # RIS çıktısını yaz
    try:
        ris_path = str(Path(output_path).with_suffix(".ris"))
        _write_output_ris(ris_path, results)
    except Exception as exc:
        log.warning("Çıktı RIS dosyası yazılamadı: %s", exc)

    # MARC21 çıktıları (.mrc + MARCXML .xml)
    try:
        _write_output_marc(
            str(Path(output_path).with_suffix(".mrc")),
            str(Path(output_path).with_suffix(".xml")),
            results,
        )
    except Exception as exc:
        log.warning("MARC çıktıları yazılamadı: %s", exc)

    found_count = sum(1 for r in results if r.get("status") == "found")
    log.info(
        "Toplu sorgu tamamlandı: %d/%d bulundu, çıktı: %s",
        found_count, len(results), output_file,
    )
    return (results, output_file)


def _query_isbn_safe(isbn: str) -> dict:
    """_query_marc'ı çağırır; beklenmeyen hatayı "error" kaydına çevirir (önbelleğe alınmaz)."""
    try:
        return _query_marc(isbn)
    except Exception as exc:
        log.error("ISBN=%s için beklenmeyen hata: %s", isbn, exc)
        return {
            "isbn": isbn,
            "title": "",
            "author": "",
            "publisher": "",
            "year": "",
            "language": "",
            "pages": "",
            "marc_020": "",
            "marc_100": "",
            "marc_245": "",
            "marc_260": "",
            "subjects": "",
            "status": "error",
            "source": "",
            "error_message": str(exc),
        }
