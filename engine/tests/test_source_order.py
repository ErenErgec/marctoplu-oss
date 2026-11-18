"""Kaynak sırası ve kaynak hatalarının ayrımı testleri. Ağ çağrısı yapılmaz.

Çalıştırma (repo kökünden):
    python3 -m unittest discover -s engine/tests -v
"""

import importlib.util
import tempfile
import unittest
from pathlib import Path
from unittest import mock

ENGINE_PATH = Path(__file__).resolve().parents[1] / "marc_toplu_sorgula.py"
spec = importlib.util.spec_from_file_location("marc_toplu_sorgula", ENGINE_PATH)
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)

SOURCE_FNS = ["_query_loc_sru", "_query_toplu_katalog", "_query_open_library", "_query_google_books"]


def _hit(source):
    return lambda isbn: {"isbn": isbn, "title": "Kitap", "status": "found", "source": source}


class SourceOrderTests(unittest.TestCase):
    def setUp(self):
        mock.patch.object(engine.time, "sleep").start()
        self.calls = []
        self.addCleanup(mock.patch.stopall)

    def _patch(self, **behaviour):
        """Her kaynak için davranış: None (bulunamadı), bir fonksiyon (sonuç) veya Exception."""
        for fn in SOURCE_FNS:
            b = behaviour.get(fn)

            def fake(isbn, fn=fn, b=b):
                self.calls.append(fn)
                if isinstance(b, Exception):
                    raise b
                return b(isbn) if callable(b) else None

            mock.patch.object(engine, fn, side_effect=fake).start()

    def test_turk_isbn_gruplari_taninir(self):
        for isbn in ["9789750719387", "9786053609421", "9786257000000", "9789944000000", "975-07-1938-X"]:
            isbn13, _ = engine._normalize_isbn(isbn)
            self.assertTrue(engine._is_turkish_isbn(isbn13), isbn)
        # ABD, Almanya, Portekiz (978-972): Türk grubu değil
        for isbn in ["9780134685991", "9783161484100", "9789720000000"]:
            self.assertFalse(engine._is_turkish_isbn(isbn), isbn)

    def test_turk_isbn_once_tokat_sorgulanir_ve_locye_gidilmez(self):
        self._patch(_query_toplu_katalog=_hit("TO-KAT"), _query_loc_sru=_hit("LOC"))
        record = engine._query_marc("978-605-360-942-1")
        self.assertEqual(record["source"], "TO-KAT")
        self.assertEqual(self.calls, ["_query_toplu_katalog"])
        self.assertEqual(record["isbn"], "978-605-360-942-1")

    def test_turk_isbn_tokatta_yoksa_diger_kaynaklara_gecer(self):
        self._patch(_query_loc_sru=_hit("LOC"))
        record = engine._query_marc("9789750719387")
        self.assertEqual(record["source"], "LOC")
        self.assertEqual(self.calls, ["_query_toplu_katalog", "_query_loc_sru"])

    def test_yabanci_isbn_sirasi_degismez(self):
        self._patch(_query_toplu_katalog=_hit("TO-KAT"))
        engine._query_marc("9780134685991")
        self.assertEqual(self.calls, ["_query_loc_sru", "_query_toplu_katalog"])

    def test_open_library_isbn10_ile_de_denenir(self):
        seen = []

        def ol(isbn):
            seen.append(isbn)
            return {"isbn": isbn, "status": "found", "source": "Open Library"} if len(isbn) == 10 else None

        self._patch(_query_open_library=ol)
        record = engine._query_marc("9780306406157")
        self.assertEqual(record["status"], "found")
        self.assertEqual(seen, ["9780306406157", "0306406152"])

    def test_temiz_bulunamadi(self):
        self._patch()
        record = engine._query_marc("9780134685991")
        self.assertEqual(record["status"], "not_found")
        self.assertNotIn("incomplete", record)

    def test_tum_kaynaklar_hata_verirse_error_doner(self):
        down = ConnectionError("ağ yok")
        self._patch(**{fn: down for fn in SOURCE_FNS})
        record = engine._query_marc("9789750719387")
        self.assertEqual(record["status"], "error")
        self.assertIn("Kaynaklara ulaşılamadı", record["error_message"])

    def test_kismi_hata_bulunamadi_ama_onbellege_alinmaz(self):
        self._patch(_query_loc_sru=TimeoutError("zaman aşımı"))
        record = engine._query_marc("9780134685991")
        self.assertEqual(record["status"], "not_found")
        self.assertTrue(record.get("incomplete"))
        self.assertIn("LOC", record["error_message"])

        with tempfile.TemporaryDirectory() as tmp:
            cache = engine.IsbnCache(str(Path(tmp) / "c.sqlite"))
            cache.put("9780134685991", record)
            self.assertIsNone(cache.get("9780134685991"))
            cache.close()


if __name__ == "__main__":
    unittest.main()
