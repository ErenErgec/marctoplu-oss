"""MARC21 çıktısı (ISO 2709 .mrc ve MARCXML) testleri. Ağ çağrısı yapılmaz, yalnızca standart kütüphane.

Çalıştırma (repo kökünden):
    python3 -m unittest discover -s engine/tests -v
"""

import importlib.util
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
from unittest import mock

ENGINE_PATH = Path(__file__).resolve().parents[1] / "marc_toplu_sorgula.py"
spec = importlib.util.spec_from_file_location("marc_toplu_sorgula", ENGINE_PATH)
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)

NS = {"m": "http://www.loc.gov/MARC21/slim"}

TR_RESULT = {
    "isbn": "978-605-360-942-1",
    "title": "Kediş'in armağanı",
    "author": "Akal, Aytül; Çiçek, Ayşe",
    "publisher": "Uçanbalık",
    "year": "2019",
    "language": "tr",
    "pages": "32",
    "subjects": "Çocuk edebiyatı; Kediler",
    "marc_020": "$a 9786053609421",
    "marc_100": "$a Akal, Aytül, $e yazar.",
    "marc_245": "$a Kediş'in armağanı",
    "marc_260": "$a İstanbul : $b Uçanbalık, $c 2019.",
    "status": "found",
    "source": "TO-KAT",
    "error_message": "",
}

LOC_XML = (
    '<record xmlns="http://www.loc.gov/MARC21/slim"><leader>01142cam  2200301 a 4500</leader>'
    '<controlfield tag="001">20033591</controlfield>'
    '<controlfield tag="008">170621s2018    maua     b    001 0 eng  </controlfield>'
    '<datafield tag="020" ind1=" " ind2=" "><subfield code="a">0134685997</subfield></datafield>'
    '<datafield tag="100" ind1="1" ind2=" "><subfield code="a">Bloch, Joshua,</subfield></datafield>'
    '<datafield tag="245" ind1="1" ind2="0"><subfield code="a">Effective Java /</subfield>'
    '<subfield code="c">Joshua Bloch.</subfield></datafield>'
    '<datafield tag="650" ind1=" " ind2="0"><subfield code="a">Java (Computer program language)</subfield></datafield>'
    "</record>"
)


def parse_iso2709(data: bytes):
    """Testler için bağımsız, minimal ISO 2709 okuyucu: [(leader, {tag: [bytes]})]."""
    records = []
    pos = 0
    while pos < len(data):
        length = int(data[pos:pos + 5])
        raw = data[pos:pos + length]
        assert raw.endswith(b"\x1d"), "kayıt sonlandırıcı yok"
        leader = raw[:24].decode("ascii")
        base = int(leader[12:17])
        directory = raw[24:base - 1]
        assert raw[base - 1:base] == b"\x1e", "dizin sonlandırıcı yok"
        fields = {}
        for i in range(0, len(directory), 12):
            entry = directory[i:i + 12].decode("ascii")
            tag, flen, start = entry[:3], int(entry[3:7]), int(entry[7:12])
            chunk = raw[base + start: base + start + flen]
            assert chunk.endswith(b"\x1e"), f"{tag} alan sonlandırıcı yok"
            fields.setdefault(tag, []).append(chunk[:-1])
        records.append((leader, fields))
        pos += length
    return records


class MarcExportTests(unittest.TestCase):
    def test_alt_alan_metni_ayristirilir(self):
        self.assertEqual(
            engine._parse_subfield_string("$a İstanbul : $b Uçanbalık, $c 2019."),
            [("a", "İstanbul :"), ("b", "Uçanbalık,"), ("c", "2019.")],
        )
        self.assertEqual(engine._parse_subfield_string(""), [])

    def test_dil_kodu_marc_bicimine_cevrilir(self):
        self.assertEqual(engine._marc_lang("tr"), "tur")
        self.assertEqual(engine._marc_lang("en-US"), "eng")
        self.assertEqual(engine._marc_lang("tur"), "tur")
        self.assertEqual(engine._marc_lang(""), "und")

    def test_sentetik_kayit_alanlari(self):
        rec = engine._build_marc_record(TR_RESULT, now=0)
        ctrl, data = rec.fields_sorted()
        ctrl = dict(ctrl)
        self.assertEqual(len(rec.leader), 24)
        self.assertEqual(ctrl["001"], "9786053609421")
        self.assertEqual(len(ctrl["008"]), 40)
        self.assertEqual(ctrl["008"][7:11], "2019")
        self.assertEqual(ctrl["008"][35:38], "tur")
        tags = [d[0] for d in data]
        self.assertEqual(tags, ["020", "100", "245", "260", "300", "500", "650", "650", "700"])
        by_tag = {d[0]: d for d in data}
        self.assertEqual(by_tag["245"][1:3], ("1", "0"))  # ana giriş var → ind1=1
        self.assertEqual(by_tag["700"][3], [("a", "Çiçek, Ayşe")])
        self.assertEqual(by_tag["300"][3], [("a", "32 s.")])
        self.assertIn("TO-KAT", by_tag["500"][3][0][1])

    def test_loc_kaydi_birebir_korunur(self):
        rec = engine._build_marc_record({**TR_RESULT, "source": "LOC", "marc_xml": LOC_XML})
        ctrl, data = rec.fields_sorted()
        self.assertEqual(dict(ctrl)["001"], "20033591")
        self.assertEqual([d[0] for d in data], ["020", "100", "245", "650"])
        self.assertNotIn("500", [d[0] for d in data])  # LoC kaydına not eklenmez

    def test_bulunamayan_ve_hatali_satir_marca_girmez(self):
        self.assertIsNone(engine._build_marc_record({**TR_RESULT, "status": "not_found"}))
        self.assertIsNone(engine._build_marc_record({**TR_RESULT, "status": "error"}))

    def test_iso2709_yapisi_ve_utf8_uzunluklari(self):
        data = engine._marc_to_iso2709(engine._build_marc_record(TR_RESULT, now=0))
        [(leader, fields)] = parse_iso2709(data)
        self.assertEqual(int(leader[:5]), len(data))  # bayt cinsinden kayıt uzunluğu
        self.assertEqual(leader[9], "a")  # UTF-8
        self.assertEqual(leader[20:24], "4500")
        title = fields["245"][0].decode("utf-8")
        self.assertEqual(title, "10\x1faKediş'in armağanı")
        self.assertEqual(fields["260"][0].decode("utf-8"), "  \x1faİstanbul :\x1fbUçanbalık,\x1fc2019.")

    def test_dosyalar_yazilir_ve_tutarli(self):
        results = [TR_RESULT, {**TR_RESULT, "status": "not_found"}, {**TR_RESULT, "source": "LOC", "marc_xml": LOC_XML}]
        with tempfile.TemporaryDirectory() as tmp:
            mrc, xml = str(Path(tmp) / "o.mrc"), str(Path(tmp) / "o.xml")
            count = engine._write_output_marc(mrc, xml, results)
            self.assertEqual(count, 2)
            self.assertEqual(len(parse_iso2709(Path(mrc).read_bytes())), 2)
            root = ET.parse(xml).getroot()
            self.assertEqual(root.tag, "{http://www.loc.gov/MARC21/slim}collection")
            titles = [r.find("m:datafield[@tag='245']/m:subfield[@code='a']", NS).text for r in root.findall("m:record", NS)]
            self.assertEqual(titles, ["Kediş'in armağanı", "Effective Java /"])

    def test_bozuk_kayit_ciktiyi_dusurmez(self):
        with tempfile.TemporaryDirectory() as tmp, mock.patch.object(engine, "_build_marc_record", side_effect=[ValueError("x"), engine._build_marc_record(TR_RESULT)]):
            count = engine._write_output_marc(str(Path(tmp) / "o.mrc"), str(Path(tmp) / "o.xml"), [TR_RESULT, TR_RESULT])
        self.assertEqual(count, 1)


class LocIsbnMatchTests(unittest.TestCase):
    """LoC kaydında yalnızca ISBN-10 olsa da ISBN-13 sorgusu eşleşmeli."""

    def _sru(self, isbn_020):
        xml = (
            '<searchRetrieveResponse xmlns="http://www.loc.gov/zing/srw/"><numberOfRecords>1</numberOfRecords>'
            "<records><record><recordData>"
            + LOC_XML.replace("0134685997", isbn_020)
            + "</recordData></record></records></searchRetrieveResponse>"
        )
        return ET.fromstring(xml)

    def test_isbn10_ek_bilgili_020_eslesir(self):
        with mock.patch.object(engine, "_http_get_xml", return_value=self._sru("0134685997 (pbk. : alk. paper)")):
            result = engine._query_loc_sru("9780134685991")
        self.assertIsNotNone(result)
        self.assertEqual(result["title"], "Effective Java")  # sondaki " /" temizlenir
        self.assertIn("<", result["marc_xml"])

    def test_tireli_020_eslesir(self):
        with mock.patch.object(engine, "_http_get_xml", return_value=self._sru("0-13-468599-7")):
            self.assertIsNotNone(engine._query_loc_sru("9780134685991"))
        with mock.patch.object(engine, "_http_get_xml", return_value=self._sru("978-0-13-468599-1 (paperback)")):
            self.assertIsNotNone(engine._query_loc_sru("9780134685991"))

    def test_farkli_isbn_reddedilir(self):
        with mock.patch.object(engine, "_http_get_xml", return_value=self._sru("0306406152")):
            self.assertIsNone(engine._query_loc_sru("9780134685991"))


if __name__ == "__main__":
    unittest.main()
