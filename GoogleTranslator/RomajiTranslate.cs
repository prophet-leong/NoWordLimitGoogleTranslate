using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Windows.Forms;

namespace RavSoft.GoogleTranslator
{
	class RomajiTranslate
	{

		private List<string> Database = new List<string>();

		public string[] GetPhrases(string text)
		{

			string phoneticText = ConvertText(text);
			
			string []temp = phoneticText.Split(new string[]{"\r\n"},StringSplitOptions.RemoveEmptyEntries);

			return temp;
		}
		public void GetDatabase()
		{
			using (System.IO.StreamReader sr = new System.IO.StreamReader("Database.txt"))
			{
				while (!sr.EndOfStream)
				{
					string splitMe = sr.ReadLine();
					Database.Add(splitMe);
				}
			}
		}

		private string ConvertText(string text)
		{
			text = text.ToLower();

			string roma = string.Empty;
			string hira = string.Empty;
			string kata = string.Empty;

			foreach (string row in Database)
			{
				var split = row.Split('@');
				roma = split[0];
				hira = split[1];
				kata = split[2];

				text = text.Replace(hira, roma);
				text = text.Replace(kata, roma);

			}
			return text;
		}
	}
}
