"""ISBN önbelleği testleri. Ağ çağrısı yapılmaz: _query_marc ve time.sleep yerine sahteleri kullanılır.

Çalıştırma (repo kökünden):
    python3 -m unittest discover -s engine/tests -v
"""

import importlib.util
import os
import sqlite3
import tempfile
import unittest
from pathlib import Path
from unittest import mock

from openpyxl import Workbook

ENGINE_PATH = Path(__file__).resolve().parents[1] / "marc_toplu_sorgula.py"
spec = importlib.util.spec_from_file_location("marc_toplu_sorgula", ENGINE_PATH)
engine = importlib.util.module_from_spec(spec)
spec.loader.exec_module(engine)


def _found(isbn):
    return {"isbn": isbn, "title": f"Kitap {isbn}", "author": "Yazar", "publisher": "", "year": "2020",
            "language": "tur", "pages": "", "marc_020": "", "marc_100": "", "marc_245": "", "marc_260": "",
            "subjects": "", "status": "found", "source": "TO-KAT", "error_message": ""}


def _not_found(isbn):
    return {**_found(isbn), "title": "", "status": "not_found", "source": "", "error_message": "Kayıt bulunamadı."}


class IsbnCacheTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.dir = Path(self.tmp.name)
        self.cache_path = str(self.dir / "_cache" / "isbn_cache.sqlite")
        self.sleep = mock.patch.object(engine.time, "sleep").start()
        self.addCleanup(mock.patch.stopall)
        self.addCleanup(self.tmp.cleanup)

    def _input(self, isbns, name="in.xlsx"):
        wb = Workbook()
        ws = wb.active
        ws.append(["ISBN"])
        for isbn in isbns:
            ws.append([isbn])
        path = self.dir / name
        wb.save(path)
        return str(path)

    def _run(self, isbns, query, cache_path="default", progress=None):
        with mock.patch.object(engine, "_query_marc", side_effect=query) as q:
            results, out = engine.run_batch(
                self._input(isbns),
                str(self.dir / "out.xlsx"),
                progress_callback=progress,
                cache_path=self.cache_path if cache_path == "default" else cache_path,
            )
        return results, out, q

    def test_ikinci_calismada_dis_kaynaga_gidilmez(self):
        isbns = ["9789750719387", "9786053609421"]
        first, _, q1 = self._run(isbns, _found)
        self.assertEqual(q1.call_count, 2)

        messages = []
        second, out, q2 = self._run(isbns, _found, progress=lambda *a: messages.append(a[4]))
        self.assertEqual(q2.call_count, 0)
        self.assertEqual([r["title"] for r in second], [r["title"] for r in first])
        self.assertTrue(all("(önbellek)" in m for m in messages))
        self.assertTrue(Path(out).exists())

    def test_onbellek_isabetinde_bekleme_yapilmaz(self):
        self._run(["9789750719387"], _found)
        self.sleep.reset_mock()
        self._run(["9789750719387"], _found)
        self.sleep.assert_not_called()

    def test_isbn10_ve_isbn13_ayni_kayit(self):
        self._run(["9780306406157"], _found)
        results, _, q = self._run(["0306406152"], _found)
        self.assertEqual(q.call_count, 0)
        self.assertEqual(results[0]["isbn"], "0306406152")  # kullanıcının yazımı korunur

    def test_hatalar_onbellege_alinmaz(self):
        def boom(isbn):
            raise TimeoutError("zaman aşımı")

        results, _, _ = self._run(["9789750719387"], boom)
        self.assertEqual(results[0]["status"], "error")
        _, _, q = self._run(["9789750719387"], _found)
        self.assertEqual(q.call_count, 1)

    def test_bulunamayanlarin_suresi_kisa(self):
        now = [1_000_000.0]
        cache = engine.IsbnCache(self.cache_path, now=lambda: now[0])
        cache.put("9789750719387", _not_found("9789750719387"))
        cache.put("9786053609421", _found("9786053609421"))

        now[0] += engine._CACHE_TTL_NOT_FOUND + 1
        self.assertIsNone(cache.get("9789750719387"))
        self.assertIsNotNone(cache.get("9786053609421"))

        now[0] += engine._CACHE_TTL_FOUND
        self.assertIsNone(cache.get("9786053609421"))
        cache.close()

    def test_onbellek_acilamazsa_is_devam_eder(self):
        blocker = self.dir / "dosya"
        blocker.write_text("x")  # dizin yerine dosya: önbellek oluşturulamaz
        results, out, q = self._run(["9789750719387"], _found, cache_path=str(blocker / "isbn_cache.sqlite"))
        self.assertEqual(q.call_count, 1)
        self.assertEqual(results[0]["status"], "found")
        self.assertTrue(Path(out).exists())

    def test_cache_path_yoksa_dosya_olusmaz(self):
        _, _, q = self._run(["9789750719387"], _found, cache_path=None)
        self.assertEqual(q.call_count, 1)
        self.assertFalse(os.path.exists(self.cache_path))

    def test_bozuk_kayit_yeniden_sorgulanir(self):
        self._run(["9789750719387"], _found)
        conn = sqlite3.connect(self.cache_path)
        conn.execute("UPDATE isbn_cache SET record_json = '{bozuk'")
        conn.commit()
        conn.close()
        _, _, q = self._run(["9789750719387"], _found)
        self.assertEqual(q.call_count, 1)


if __name__ == "__main__":
    unittest.main()
