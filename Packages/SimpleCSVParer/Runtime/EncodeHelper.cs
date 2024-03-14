using System.IO;
using System.Text;

namespace Xeon.IO
{
    internal static class EncodeHelper
    {
        /// <summary>
        /// 日本語文字コードを取得する
        /// <see href="https://qiita.com/nekotadon/items/c1478b5655755018c67c"/>
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="file">ファイルパス</param>
        /// <param name="maxSize">最大読み取りバイト数</param>
        /// <returns></returns>
        internal static Encoding GetJpEncoding(string file, long maxSize = 50 * 1024)//ファイルパス、最大読み取りバイト数
        {
            try
            {
                if (!File.Exists(file)) return null;
                if (new FileInfo(file).Length == 0) return null;

                //バイナリ読み込み
                byte[] bytes = null;
                var readAll = false;
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var size = fs.Length;

                    if (size <= maxSize)
                    {
                        bytes = new byte[size];
                        fs.Read(bytes, 0, (int)size);
                        readAll = true;
                    }
                    else
                    {
                        bytes = new byte[maxSize];
                        fs.Read(bytes, 0, (int)maxSize);
                    }
                }
                //判定
                return GetJpEncoding(bytes, readAll);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// TODO: 古い記法で書かれているコードなので、時間のあるときにリファクタリングする
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="readAll"></param>
        /// <returns></returns>
        private static Encoding GetJpEncoding(byte[] bytes, bool readAll = false)
        {
            var len = bytes.Length;

            //BOM判定
            if (len >= 2 && bytes[0] == 0xfe && bytes[1] == 0xff)//UTF-16BE
                return Encoding.BigEndianUnicode;

            if (len >= 2 && bytes[0] == 0xff && bytes[1] == 0xfe)//UTF-16LE
                return Encoding.Unicode;

            if (len >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)//UTF-8
                return new UTF8Encoding(true, true);

            if (len >= 3 && bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76)//UTF-7
                return Encoding.UTF7;

            else if (len >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xfe && bytes[3] == 0xff)//UTF-32BE
                return new UTF32Encoding(true, true);

            else if (len >= 4 && bytes[0] == 0xff && bytes[1] == 0xfe && bytes[2] == 0x00 && bytes[3] == 0x00)//UTF-32LE
                return new UTF32Encoding(false, true);

            //文字コード判定と日本語の文章らしさをまとめて確認

            //Shift_JIS判定用
            var sjis = true;         //すべてのバイトがShift_JISで使用するバイト範囲かどうか
            var sjis_2ndbyte = false;//次回の判定がShift_JISの2バイト目の判定かどうか
            var sjis_kana = false;   //かな判定用
            var sjis_kanji = false;  //常用漢字判定用
            var counter_sjis = 0;     //Shift_JISらしさ

            //UTF-8判定用
            var utf8 = true;            //すべてのバイトがUTF-8で使用するバイト範囲かどうか
            var utf8_multibyte = false; //次回の判定がUTF-8の2バイト目以降の判定かどうか
            var utf8_kana_kanji = false;//かな・常用漢字判定用
            var counter_utf8 = 0;        //UTF-8らしさ
            var counter_utf8_multibyte = 0;

            //EUC-JP判定用
            var eucjp = true;            //すべてのバイトがEUC-JPで使用するバイト範囲かどうか
            var eucjp_multibyte = false; //次回の判定がEUC-JPの2バイト目以降の判定かどうか
            var eucjp_kana_kanji = false;//かな・常用漢字判定用
            var counter_eucjp = 0;        //EUC-JPらしさ
            var counter_eucjp_multibyte = 0;

            for (var i = 0; i < len; i++)
            {
                var b = bytes[i];

                //Shift_JIS判定
                if (sjis)
                {
                    if (!sjis_2ndbyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))//ASCII文字
                        {
                            counter_sjis++;
                        }
                        else if ((0x81 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xFC))//Shift_JISの2バイト文字の1バイト目の場合
                        {
                            //2バイト目の判定を行う
                            sjis_2ndbyte = true;

                            if (0x82 <= b && b <= 0x83)//Shift_JISのかな
                            {
                                sjis_kana = true;
                            }
                            else if ((0x88 <= b && b <= 0x9F) || (0xE0 <= b && b <= 0xE3) || b == 0xE6 || b == 0xE7)//Shift_JISの常用漢字
                            {
                                sjis_kanji = true;
                            }
                        }
                        else if (0xA1 <= b && b <= 0xDF)//Shift_JISの1バイト文字の場合(半角カナ)
                        {
                            ;
                        }
                        else if (0x00 <= b && b <= 0x7F)//ASCIIコード
                        {
                            ;
                        }
                        else
                        {
                            //Shift_JISでない
                            counter_sjis = 0;
                            sjis = false;
                        }
                    }
                    else
                    {
                        if ((0x40 <= b && b <= 0x7E) || (0x80 <= b && b <= 0xFC))//Shift_JISの2バイト文字の2バイト目の場合
                        {
                            if (sjis_kana && 0x40 <= b && b <= 0xF1)//Shift_JISのかな
                            {
                                counter_sjis += 2;
                            }
                            else if (sjis_kanji && 0x40 <= b && b <= 0xFC && b != 0x7F)//Shift_JISの常用漢字
                            {
                                counter_sjis += 2;
                            }

                            sjis_2ndbyte = sjis_kana = sjis_kanji = false;
                        }
                        else
                        {
                            //Shift_JISでない
                            counter_sjis = 0;
                            sjis = false;
                        }
                    }
                }

                //UTF-8判定
                if (utf8)
                {
                    if (!utf8_multibyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))//ASCII文字
                        {
                            counter_utf8++;
                        }
                        else if (0xC2 <= b && b <= 0xDF)//2バイト文字の場合
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 1;
                        }
                        else if (0xE0 <= b && b <= 0xEF)//3バイト文字の場合
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 2;

                            if (b == 0xE3 || (0xE4 <= b && b <= 0xE9))
                            {
                                utf8_kana_kanji = true;//かな・常用漢字
                            }
                        }
                        else if (0xF0 <= b && b <= 0xF3)//4バイト文字の場合
                        {
                            utf8_multibyte = true;
                            counter_utf8_multibyte = 3;
                        }
                        else if (0x00 <= b && b <= 0x7F)//ASCIIコード
                        {
                            ;
                        }
                        else
                        {
                            //UTF-8でない
                            counter_utf8 = 0;
                            utf8 = false;
                        }
                    }
                    else
                    {
                        if (counter_utf8_multibyte > 0)
                        {
                            counter_utf8_multibyte--;

                            if (b < 0x80 || 0xBF < b)
                            {
                                //UTF-8でない
                                counter_utf8 = 0;
                                utf8 = false;
                            }
                        }

                        if (utf8 && counter_utf8_multibyte == 0)
                        {
                            if (utf8_kana_kanji)
                            {
                                counter_utf8 += 3;
                            }
                            utf8_multibyte = utf8_kana_kanji = false;
                        }
                    }
                }

                //EUC-JP判定
                if (eucjp)
                {
                    if (!eucjp_multibyte)
                    {
                        if (b == 0x0D                   //CR
                            || b == 0x0A                //LF
                            || b == 0x09                //tab
                            || (0x20 <= b && b <= 0x7E))//ASCII文字
                        {
                            counter_eucjp++;
                        }
                        else if (b == 0x8E || (0xA1 <= b && b <= 0xA8) || b == 0xAD || (0xB0 <= b && b <= 0xFE))//2バイト文字の場合
                        {
                            eucjp_multibyte = true;
                            counter_eucjp_multibyte = 1;

                            if (b == 0xA4 || b == 0xA5 || (0xB0 <= b && b <= 0xEE))
                            {
                                eucjp_kana_kanji = true;
                            }
                        }
                        else if (b == 0x8F)//3バイト文字の場合
                        {
                            eucjp_multibyte = true;
                            counter_eucjp_multibyte = 2;
                        }
                        else if (0x00 <= b && b <= 0x7F)//ASCIIコード
                        {
                            ;
                        }
                        else
                        {
                            //EUC-JPでない
                            counter_eucjp = 0;
                            eucjp = false;
                        }
                    }
                    else
                    {
                        if (counter_eucjp_multibyte > 0)
                        {
                            counter_eucjp_multibyte--;

                            if (b < 0xA1 || 0xFE < b)
                            {
                                //EUC-JPでない
                                counter_eucjp = 0;
                                eucjp = false;
                            }
                        }

                        if (eucjp && counter_eucjp_multibyte == 0)
                        {
                            if (eucjp_kana_kanji)
                            {
                                counter_eucjp += 2;
                            }
                            eucjp_multibyte = eucjp_kana_kanji = false;
                        }
                    }
                }

                //ISO-2022-JP
                if (b == 0x1B)
                {
                    if ((i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x40)                                                                           //1B-24-40
                        || (i + 2 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x42)                                                                        //1B-24-42
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x4A)                                                                        //1B-28-4A
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x49)                                                                        //1B-28-49
                        || (i + 2 < len && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x42)                                                                        //1B-28-42
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x44)                                                //1B-24-48-44
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x4F)                                                //1B-24-48-4F
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x51)                                                //1B-24-48-51
                        || (i + 3 < len && bytes[i + 1] == 0x24 && bytes[i + 2] == 0x48 && bytes[i + 3] == 0x50)                                                //1B-24-48-50
                        || (i + 5 < len && bytes[i + 1] == 0x26 && bytes[i + 2] == 0x40 && bytes[i + 3] == 0x1B && bytes[i + 4] == 0x24 && bytes[i + 5] == 0x42)//1B-26-40-1B-24-42
                    )
                    {
                        return Encoding.GetEncoding(50220);//iso-2022-jp
                    }
                }
            }

            // すべて読み取った場合で、最後が多バイト文字の途中で終わっている場合は判定NG
            if (readAll)
            {
                if (sjis && sjis_2ndbyte)
                {
                    sjis = false;
                }

                if (utf8 && utf8_multibyte)
                {
                    utf8 = false;
                }

                if (eucjp && eucjp_multibyte)
                {
                    eucjp = false;
                }
            }

            if (sjis || utf8 || eucjp)
            {
                //日本語らしさの最大値確認
                int max_value = counter_eucjp;
                if (counter_sjis > max_value)
                {
                    max_value = counter_sjis;
                }
                if (counter_utf8 > max_value)
                {
                    max_value = counter_utf8;
                }

                //文字コード判定
                if (max_value == counter_utf8)
                    return new UTF8Encoding(false, true);//utf8
                if (max_value == counter_sjis)
                    return Encoding.GetEncoding(932);//ShiftJIS
                else
                    return Encoding.GetEncoding(51932);//EUC-JP
            }
            else
            {
                return null;
            }
        }
    }
}
