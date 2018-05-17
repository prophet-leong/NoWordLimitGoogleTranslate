using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms;
using HtmlAgilityPack;


namespace RavSoft.GoogleTranslator
{
	class RomajiTranslate
	{

		public const string LanguagePair = "ja|en";

		private const string TranslatorUrl = "https://www.google.com/translate_t?hl=en&ie=UTF8&text={0}&langpair={1}";

		public const string fullStop = "   ";

		public static string GetTranslatorUrl(string text, string languagePair = LanguagePair)
		{
			return string.Format(TranslatorUrl, text, languagePair);
		}

		public static string[] GetPhrases(string text, string languagePair = LanguagePair)
		{

			string url = RomajiTranslate.GetTranslatorUrl(text, languagePair);
            HtmlAgilityPack.HtmlDocument doc = new HtmlWeb().Load(url);
            string phoneticText = HttpUtility.HtmlDecode(doc.GetElementbyId("src-translit").InnerText);
			
			string []temp = phoneticText.Split(new string[]{"."},StringSplitOptions.None);

			return temp;
		}
	}
}
