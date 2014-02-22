using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RTMPDownloader
{
	public class Utilidades
	{
		public static int UnixTimestamp()
		{
			return (int)(DateTime.Now - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds;
		}

		public static string ReemplazaParametro(string input, string parameter, string replacement){
			string pattern = "-"+parameter+" ((\".*?\")|([^ ]*))";
			Regex rgx = new Regex(pattern);
			return rgx.Replace(input, "-"+parameter+" \""+replacement+"\" ");
		}

		public static string GetParametro(string input, string parameter){
			string pattern = "-"+parameter+" ((\".*?\")|([^ ]*))";
			Regex rgx = new Regex(pattern);
			MatchCollection matches = rgx.Matches(input);
			if(matches.Count > 0){
				if(matches[0].Groups.Count > 1 && matches[0].Groups[1].Value != "")
					return matches[0].Groups[1].Value.Substring(1, matches[0].Groups[1].Value.Length-2);
				else
					return matches[0].Value;
			}
			return "";
		}
		
		public static string WL(string txt){
			using (StreamWriter sw  = File.AppendText(@"DEBUG.txt") ){
				sw.WriteLine(txt);
			}
			Console.WriteLine(txt);
			return txt;
		}
	}
}