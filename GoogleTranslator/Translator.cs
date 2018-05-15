﻿// Copyright (c) 2015 Ravi Bhavnani
// License: Code Project Open License
// http://www.codeproject.com/info/cpol10.aspx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace RavSoft.GoogleTranslator
{
    /// <summary>
    /// Translates text using Google's online language tools.
    /// </summary>
    public class Translator
    {
		const string fullstop = "。";

        #region Properties

            /// <summary>
            /// Gets the supported languages.
            /// </summary>
            public static IEnumerable<string> Languages {
                get {
                    Translator.EnsureInitialized();
                    return Translator._languageModeMap.Keys.OrderBy (p => p);
                }
            }

            /// <summary>
            /// Gets the time taken to perform the translation.
            /// </summary>
            public TimeSpan TranslationTime {
                get;
                private set;
            }

            /// <summary>
            /// Gets the url used to speak the translation.
            /// </summary>
            /// <value>The url used to speak the translation.</value>
            public string TranslationSpeechUrl {
                get;
                private set;
            }

            /// <summary>
            /// Gets the error.
            /// </summary>
            public Exception Error {
                get;
                private set;
            }

        #endregion

        #region Public methods

            /// <summary>
            /// Translates the specified source text.
            /// </summary>
            /// <param name="sourceText">The source text.</param>
            /// <param name="sourceLanguage">The source language.</param>
            /// <param name="targetLanguage">The target language.</param>
            /// <returns>The translation.</returns>
            public string Translate(string sourceText, string sourceLanguage, string targetLanguage, string referenceTextUrl)
            {
                // Initialize
                this.Error = null;
                this.TranslationSpeechUrl = null;
                this.TranslationTime = TimeSpan.Zero;
                DateTime tmStart = DateTime.Now;
                string translation = string.Empty;

				//seperate text by fullstops
				sourceText = SegmentingSentences(sourceText);

				if(File.Exists(referenceTextUrl))
				{
					string text = File.ReadAllText(referenceTextUrl);
					string[] phrases = text.Split(new[] { ",\r\n" }, StringSplitOptions.None);
					for(int i =0;i<phrases.Length;++i)
					{
						string[] replace = phrases[i].Split(new[] { ":" }, StringSplitOptions.None);
						sourceText = sourceText.Replace(replace[0], replace[1]);
					}
				}
				List<string> temp = new List<string>();
				
				while(sourceText.Length > 150)
				{
					string tempstr = sourceText.Substring(0,150);
					int substrSize = tempstr.LastIndexOf(fullstop)+1;
					tempstr = tempstr.Substring(0,substrSize);
					temp.Add(tempstr);
					sourceText = sourceText.Remove(0, tempstr.Length);
				}
				temp.Add(sourceText);
				for (int i = 0; i < temp.Count; ++i)
				{
					translation += Translating(temp[i],sourceLanguage,targetLanguage);
					translation += "\r\n";
				}




				
				this.TranslationTime = DateTime.Now - tmStart;
                return translation;
				#endregion
            }
		string Translating(string sourceText , string sourceLanguage, string targetLanguage)
		{
				string translation = string.Empty;
				try
				{
					// Download translation
					string url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}",
												Translator.LanguageEnumToIdentifier(sourceLanguage),
												Translator.LanguageEnumToIdentifier(targetLanguage),
												HttpUtility.UrlEncode(sourceText));
					string outputFile = Path.GetTempFileName();
					using (WebClient wc = new WebClient())
					{
						wc.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2228.0 Safari/537.36");
						wc.DownloadFile(url, outputFile);
					}
					// Get translated text
					if (File.Exists(outputFile))
					{
						// Get phrase collection
						string text = File.ReadAllText(outputFile);
						text = text.Remove(0, 2);
						text = text.Replace(",null,\"" + Translator.LanguageEnumToIdentifier(sourceLanguage) + "\"", "");
						string[] phrases = text.Split(new[] { "]," }, StringSplitOptions.None);
						string[] rawPhrases = sourceText.Split(new[] { "\r\n" }, StringSplitOptions.None);
						int s = 2;
						int len;
						for (int i = 0;i<phrases.Length;++i)
						{
							len = phrases[i].IndexOf("\",\"") - 2;
							translation += phrases[i].Substring(s, len);
						}
						// clean up first
						translation = translation.Replace("\\r", "\r");
						translation = translation.Replace("\\n", "\n");
						translation = translation.Replace("\\\"", "\"");
						string[] translatedPhrases = translation.Split(new[] { "\r\n" }, StringSplitOptions.None);

						int highestValue = Math.Max(translatedPhrases.Length, rawPhrases.Length);
						translation = string.Empty;
						for (int i = 0; i < highestValue; ++i)
						{
							
							//to prevent an exception from happening
							if (rawPhrases.Length > i)
								translation += rawPhrases[i] + "\r\n";
							if (translatedPhrases.Length > i)
								translation += translatedPhrases[i] + "\r\n";
						}
					}
				}
				catch (Exception ex)
				{
					this.Error = ex;
				}
				return translation;
			}
		string SegmentingSentences(string sourceText)
		{
			int i = sourceText.LastIndexOf(fullstop);
			int currIndex = 0;
			while(true)
			{
				currIndex = sourceText.IndexOf(fullstop, currIndex+1);
				if (i != currIndex)
				{
					char temp = sourceText[currIndex+1];
					if(temp != fullstop[0] || temp != '\r' )
					{
						sourceText.Insert(currIndex, "\r\n");
					}
				}
				else
					break;
				
			}
			return sourceText;
		}
        #region Private methods

            /// <summary>
            /// Converts a language to its identifier.
            /// </summary>
            /// <param name="language">The language."</param>
            /// <returns>The identifier or <see cref="string.Empty"/> if none.</returns>
            private static string LanguageEnumToIdentifier
                (string language)
            {
                string mode = string.Empty;
                Translator.EnsureInitialized();
                Translator._languageModeMap.TryGetValue (language, out mode);
                return mode;
            }

            /// <summary>
            /// Ensures the translator has been initialized.
            /// </summary>
            private static void EnsureInitialized()
            {
                if (Translator._languageModeMap == null) {
                    Translator._languageModeMap = new Dictionary<string,string>();
                    Translator._languageModeMap.Add ("Afrikaans",   "af");
                    Translator._languageModeMap.Add ("Albanian",    "sq");
                    Translator._languageModeMap.Add ("Arabic",      "ar");
                    Translator._languageModeMap.Add ("Armenian",    "hy");
                    Translator._languageModeMap.Add ("Azerbaijani", "az");
                    Translator._languageModeMap.Add ("Basque",      "eu");
                    Translator._languageModeMap.Add ("Belarusian",  "be");
                    Translator._languageModeMap.Add ("Bengali",     "bn");
                    Translator._languageModeMap.Add ("Bulgarian",   "bg");
                    Translator._languageModeMap.Add ("Catalan",     "ca");
                    Translator._languageModeMap.Add ("Chinese",     "zh-CN");
                    Translator._languageModeMap.Add ("Croatian",    "hr");
                    Translator._languageModeMap.Add ("Czech",       "cs");
                    Translator._languageModeMap.Add ("Danish",      "da");
                    Translator._languageModeMap.Add ("Dutch",       "nl");
                    Translator._languageModeMap.Add ("English",     "en");
                    Translator._languageModeMap.Add ("Esperanto",   "eo");
                    Translator._languageModeMap.Add ("Estonian",    "et");
                    Translator._languageModeMap.Add ("Filipino",    "tl");
                    Translator._languageModeMap.Add ("Finnish",     "fi");
                    Translator._languageModeMap.Add ("French",      "fr");
                    Translator._languageModeMap.Add ("Galician",    "gl");
                    Translator._languageModeMap.Add ("German",      "de");
                    Translator._languageModeMap.Add ("Georgian",    "ka");
                    Translator._languageModeMap.Add ("Greek",       "el");
                    Translator._languageModeMap.Add ("Haitian Creole",    "ht");
                    Translator._languageModeMap.Add ("Hebrew",      "iw");
                    Translator._languageModeMap.Add ("Hindi",       "hi");
                    Translator._languageModeMap.Add ("Hungarian",   "hu");
                    Translator._languageModeMap.Add ("Icelandic",   "is");
                    Translator._languageModeMap.Add ("Indonesian",  "id");
                    Translator._languageModeMap.Add ("Irish",       "ga");
                    Translator._languageModeMap.Add ("Italian",     "it");
                    Translator._languageModeMap.Add ("Japanese",    "ja");
                    Translator._languageModeMap.Add ("Korean",      "ko");
                    Translator._languageModeMap.Add ("Lao",         "lo");
                    Translator._languageModeMap.Add ("Latin",       "la");
                    Translator._languageModeMap.Add ("Latvian",     "lv");
                    Translator._languageModeMap.Add ("Lithuanian",  "lt");
                    Translator._languageModeMap.Add ("Macedonian",  "mk");
                    Translator._languageModeMap.Add ("Malay",       "ms");
                    Translator._languageModeMap.Add ("Maltese",     "mt");
                    Translator._languageModeMap.Add ("Norwegian",   "no");
                    Translator._languageModeMap.Add ("Persian",     "fa");
                    Translator._languageModeMap.Add ("Polish",      "pl");
                    Translator._languageModeMap.Add ("Portuguese",  "pt");
                    Translator._languageModeMap.Add ("Romanian",    "ro");
                    Translator._languageModeMap.Add ("Russian",     "ru");
                    Translator._languageModeMap.Add ("Serbian",     "sr");
                    Translator._languageModeMap.Add ("Slovak",      "sk");
                    Translator._languageModeMap.Add ("Slovenian",   "sl");
                    Translator._languageModeMap.Add ("Spanish",     "es");
                    Translator._languageModeMap.Add ("Swahili",     "sw");
                    Translator._languageModeMap.Add ("Swedish",     "sv");
                    Translator._languageModeMap.Add ("Tamil",       "ta");
                    Translator._languageModeMap.Add ("Telugu",      "te");
                    Translator._languageModeMap.Add ("Thai",        "th");
                    Translator._languageModeMap.Add ("Turkish",     "tr");
                    Translator._languageModeMap.Add ("Ukrainian",   "uk");
                    Translator._languageModeMap.Add ("Urdu",         "ur");
                    Translator._languageModeMap.Add ("Vietnamese",  "vi");
                    Translator._languageModeMap.Add ("Welsh",       "cy");
                    Translator._languageModeMap.Add ("Yiddish",     "yi");
                }
            }

        #endregion

        #region Fields

            /// <summary>
            /// The language to translation mode map.
            /// </summary>
            private static Dictionary<string, string> _languageModeMap;

        #endregion
    }
}