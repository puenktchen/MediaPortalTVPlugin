using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    class TranslateMediainfoToEmby
    {
        public static string MediaCodec(string mediaCodec)
        {
            Dictionary<string, string> mediaCodecMapping = new Dictionary<string, string>()
            {
                { "MPEG-TS", "ts" },
                { "MPEG-2 Video", "MPEG2VIDEO" },
                { "AVC", "H264" },
                { "MPEG-1 Audio layer 2", "MP2" },
                { "AC3+", "AC3" },
                { "DVB Subtitle", "dvbsub"},
                { "Teletext", "Subtitle"},
            };

            string codec;
            if (mediaCodecMapping.TryGetValue(mediaCodec, out codec))
            {
                mediaCodec = codec;
            }
            return mediaCodec;
        }

        public static string AudioLanguage(string audioLanguage)
        {
            Dictionary<string, string> countryCodesMapping = new Dictionary<string, string>()
            {
                { "aa", "aar" },    // Afar
                { "ab", "abk" },    // Abkhazian
                { "af", "afr" },    // Afrikaans
                { "ak", "aka" },    // Akan
                { "sq", "alb" },    // Albanian
                { "am", "amh" },    // Amharic
                { "ar", "ara" },    // Arabic
                { "an", "arg" },    // Aragonese
                { "hy", "arm" },    // Armenian
                { "as", "asm" },    // Assamese
                { "av", "ava" },    // Avaric
                { "ae", "ave" },    // Avestan
                { "ay", "aym" },    // Aymara
                { "az", "aze" },    // Azerbaijani
                { "ba", "bak" },    // Bashkir
                { "bm", "bam" },    // Bambara
                { "eu", "baq" },    // Basque
                { "be", "bel" },    // Belarusian
                { "bn", "ben" },    // Bengali
                { "bh", "bih" },    // Bihari languages
                { "bi", "bis" },    // Bislama
                { "bs", "bos" },    // Bosnian
                { "br", "bre" },    // Breton
                { "bg", "bul" },    // Bulgarian
                { "my", "bur" },    // Burmese
                { "ca", "cat" },    // Catalan; Valencian
                { "ch", "cha" },    // Chamorro
                { "ce", "che" },    // Chechen
                { "zh", "chi" },    // Chinese
                { "cu", "chu" },    // Church Slavic; Old Slavonic; Church Slavonic; Old Bulgarian; Old Church Slavonic
                { "cv", "chv" },    // Chuvash
                { "kw", "cor" },    // Cornish
                { "co", "cos" },    // Corsican
                { "cr", "cre" },    // Cree
                { "cs", "cze" },    // Czech
                { "da", "dan" },    // Danish
                { "dv", "div" },    // Divehi; Dhivehi; Maldivian
                { "nl", "dut" },    // Dutch; Flemish
                { "dz", "dzo" },    // Dzongkha
                { "en", "eng" },    // English
                { "eo", "epo" },    // Esperanto
                { "et", "est" },    // Estonian
                { "ee", "ewe" },    // Ewe
                { "fo", "fao" },    // Faroese
                { "fj", "fij" },    // Fijian
                { "fi", "fin" },    // Finnish
                { "fr", "fre" },    // French
                { "fy", "fry" },    // Western Frisian
                { "ff", "ful" },    // Fulah
                { "ka", "geo" },    // Georgian
                { "de", "ger" },    // German
                { "gd", "gla" },    // Gaelic; Scottish Gaelic
                { "ga", "gle" },    // Irish
                { "gl", "glg" },    // Galician
                { "gv", "glv" },    // Manx
                { "el", "gre" },    // Greek, Modern (1453-)
                { "gn", "grn" },    // Guarani
                { "gu", "guj" },    // Gujarati
                { "ht", "hat" },    // Haitian; Haitian Creole
                { "ha", "hau" },    // Hausa
                { "he", "heb" },    // Hebrew
                { "hz", "her" },    // Herero
                { "hi", "hin" },    // Hindi
                { "ho", "hmo" },    // Hiri Motu
                { "hr", "hrv" },    // Croatian
                { "hu", "hun" },    // Hungarian
                { "ig", "ibo" },    // Igbo
                { "is", "ice" },    // Icelandic
                { "io", "ido" },    // Ido
                { "ii", "iii" },    // Sichuan Yi; Nuosu
                { "iu", "iku" },    // Inuktitut
                { "ie", "ile" },    // Interlingue; Occidental
                { "ia", "ina" },    // Interlingua (International Auxiliary Language Association)
                { "id", "ind" },    // Indonesian
                { "ik", "ipk" },    // Inupiaq
                { "it", "ita" },    // Italian
                { "jv", "jav" },    // Javanese
                { "ja", "jpn" },    // Japanese
                { "kl", "kal" },    // Kalaallisut; Greenlandic
                { "kn", "kan" },    // Kannada
                { "ks", "kas" },    // Kashmiri
                { "kr", "kau" },    // Kanuri
                { "kk", "kaz" },    // Kazakh
                { "km", "khm" },    // Central Khmer
                { "ki", "kik" },    // Kikuyu; Gikuyu
                { "rw", "kin" },    // Kinyarwanda
                { "ky", "kir" },    // Kirghiz; Kyrgyz
                { "kv", "kom" },    // Komi
                { "kg", "kon" },    // Kongo
                { "ko", "kor" },    // Korean
                { "kj", "kua" },    // Kuanyama; Kwanyama
                { "ku", "kur" },    // Kurdish
                { "lo", "lao" },    // Lao
                { "la", "lat" },    // Latin
                { "lv", "lav" },    // Latvian
                { "li", "lim" },    // Limburgan; Limburger; Limburgish
                { "ln", "lin" },    // Lingala
                { "lt", "lit" },    // Lithuanian
                { "lb", "ltz" },    // Luxembourgish; Letzeburgesch
                { "lu", "lub" },    // Luba-Katanga
                { "lg", "lug" },    // Ganda
                { "mk", "mac" },    // Macedonian
                { "mh", "mah" },    // Marshallese
                { "ml", "mal" },    // Malayalam
                { "mi", "mao" },    // Maori
                { "mr", "mar" },    // Marathi
                { "ms", "may" },    // Malay
                { "mg", "mlg" },    // Malagasy
                { "mt", "mlt" },    // Maltese
                { "mn", "mon" },    // Mongolian
                { "na", "nau" },    // Nauru
                { "nv", "nav" },    // Navajo; Navaho
                { "nr", "nbl" },    // Ndebele, South; South Ndebele
                { "nd", "nde" },    // Ndebele, North; North Ndebele
                { "ng", "ndo" },    // Ndonga
                { "ne", "nep" },    // Nepali
                { "nn", "nno" },    // Norwegian Nynorsk; Nynorsk, Norwegian
                { "nb", "nob" },    // Bokmål, Norwegian; Norwegian Bokmål
                { "no", "nor" },    // Norwegian
                { "ny", "nya" },    // Chichewa; Chewa; Nyanja
                { "oc", "oci" },    // Occitan (post 1500); Provençal
                { "oj", "oji" },    // Ojibwa
                { "or", "ori" },    // Oriya
                { "om", "orm" },    // Oromo
                { "os", "oss" },    // Ossetian; Ossetic
                { "pa", "pan" },    // Panjabi; Punjabi
                { "fa", "per" },    // Persian
                { "pi", "pli" },    // Pali
                { "pl", "pol" },    // Polish
                { "pt", "por" },    // Portuguese
                { "ps", "pus" },    // Pushto; Pashto
                { "qu", "que" },    // Quechua
                { "rm", "roh" },    // Romansh
                { "ro", "rum" },    // Romanian; Moldavian; Moldovan
                { "rn", "run" },    // Rundi
                { "ru", "rus" },    // Russian
                { "sg", "sag" },    // Sango
                { "sa", "san" },    // Sanskrit
                { "si", "sin" },    // Sinhala; Sinhalese
                { "sk", "slo" },    // Slovak
                { "sl", "slv" },    // Slovenian
                { "se", "sme" },    // Northern Sami
                { "sm", "smo" },    // Samoan
                { "sn", "sna" },    // Shona
                { "sd", "snd" },    // Sindhi
                { "so", "som" },    // Somali
                { "st", "sot" },    // Sotho, Southern
                { "es", "spa" },    // Spanish; Castilian
                { "sc", "srd" },    // Sardinian
                { "sr", "srp" },    // Serbian
                { "ss", "ssw" },    // Swati
                { "su", "sun" },    // Sundanese
                { "sw", "swa" },    // Swahili
                { "sv", "swe" },    // Swedish
                { "ty", "tah" },    // Tahitian
                { "ta", "tam" },    // Tamil
                { "tt", "tat" },    // Tatar
                { "te", "tel" },    // Telugu
                { "tg", "tgk" },    // Tajik
                { "tl", "tgl" },    // Tagalog
                { "th", "tha" },    // Thai
                { "bo", "tib" },    // Tibetan
                { "ti", "tir" },    // Tigrinya
                { "to", "ton" },    // Tonga (Tonga Islands)
                { "tn", "tsn" },    // Tswana
                { "ts", "tso" },    // Tsonga
                { "tk", "tuk" },    // Turkmen
                { "tr", "tur" },    // Turkish
                { "tw", "twi" },    // Twi
                { "ug", "uig" },    // Uighur; Uyghur
                { "uk", "ukr" },    // Ukrainian
                { "ur", "urd" },    // Urdu
                { "uz", "uzb" },    // Uzbek
                { "ve", "ven" },    // Venda
                { "vi", "vie" },    // Vietnamese
                { "vo", "vol" },    // Volapük
                { "cy", "wel" },    // Welsh
                { "wa", "wln" },    // Walloon
                { "wo", "wol" },    // Wolof
                { "xh", "xho" },    // Xhosa
                { "yi", "yid" },    // Yiddish
                { "yo", "yor" },    // Yoruba
                { "za", "zha" },    // Zhuang; Chuang
                { "zu", "zul" },    // Zulu
            };

            string languageCode;
            if (countryCodesMapping.TryGetValue(audioLanguage, out languageCode))
            {
                audioLanguage = languageCode;
            }
            return audioLanguage;
        } 

    }
}

