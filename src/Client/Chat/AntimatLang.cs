using System;
using System.Collections.Generic;
using System.Text;

namespace Client.Chat {

	/// <summary>
	/// Упрощенный вариант "AntimatRu" для иностранных языков
	/// просто по списку запрещенных слов
	/// </summary>
	public class AntimatLang : IAntimat
	{

		public enum Language {
			Unknown,
			English,
			Deutsch,
			Spanish
		}

		/// <summary>
		/// На что заменять матерные слова
		/// </summary>
		public readonly string Censored;

		/// <summary>
		/// По какому шаблону была последняя замена (используется при анализе ложных срабатываний)
		/// </summary>
		public string LastUsedPattern { get; private set; }

		public string RemoveMat(string text) {
			int textLength = text.Length;
			if (textLength > 0xFFFF)
				throw new Exception("Very long text");

			char lastCh = ' ';
			bool newWord = true;
			int wordStartIndex = 0;
			_sb.Length = 0;
			_sb2.Length = 0;
			_tmpWordSplits.Clear();
			for (int i = 0; i < textLength; i++) {
				char ch;
				if (_repl.TryGetValue(text[i], out ch)) { // слово продолжается
					_sb2.Append(ch);
					if (ch == lastCh) // все дублирования смысловых букв убираем
						continue;
					if (newWord) { // слово только началось
						wordStartIndex = i;
						newWord = false;
					}
					_sb.Append(ch);
					lastCh = ch;
				} else if (!newWord) { // закончилось слово
					_tmpWordSplits.Add(new WordSplit {
						Begin = wordStartIndex,
						End = i,
						Word = _sb.ToString(),
						Word2 = _sb2.ToString()
					});
					_sb.Length = 0;
					_sb2.Length = 0;
					newWord = true;
					lastCh = ' ';
				}
			}
			if (_sb.Length > 0)
				_tmpWordSplits.Add(new WordSplit {
					Begin = wordStartIndex,
					End = textLength,
					Word = _sb.ToString(),
					Word2 = _sb2.ToString()
				});

			var max = _tmpWordSplits.Count - 1;
			for (int index = max; index >= 0; index--) {
				// движемся с конца в начало, иначе у нас индексы сместятся при замене текста
				_sb.Length = 0;
				_sb2.Length = 0;
				for (int i = index; i <= max && _sb.Length < 20; i++) {
					_sb.Append(_tmpWordSplits[i].Word);
					_sb2.Append(_tmpWordSplits[i].Word2);
					bool doReplace = false;
					var s = _sb.ToString();
					if (_abusive.Contains(s)) {
						LastUsedPattern = s;
						doReplace = true;
					}
					s = _sb2.ToString();
					if (_abusive.Contains(s)) {
						LastUsedPattern = s;
						doReplace = true;
					}
					if (doReplace) { // замена
						int startIndex = _tmpWordSplits[index].Begin;
						int endIndex = _tmpWordSplits[index].End;
						var newSubstring = Censored ?? "<" + text.Substring(startIndex, endIndex - startIndex).ToUpper() + ">";
						text = text.Substring(0, startIndex) + newSubstring + text.Substring(endIndex);
					}
				}
			}
			return text;
		}

		public AntimatLang(Language lang, string censored = "***") {
			if (_repl == null) {
				_repl = new Dictionary<char, char>();
				int textLength = Repl1.Length;
				for (int i = 0; i < textLength; i++) {
					//if (_repl.ContainsKey(Repl1[i])) Console.WriteLine("Duplicare '" + Repl1[i] + "'");
					_repl.Add(Repl1[i], Repl2[i]);
				}
			}
			_abusive = new HashSet<string>();
			string[] abusive = GetAbusiveArray(lang);
			if (abusive != null) 
				foreach (var s in abusive)
					_abusive.Add(s);
			_sb = new StringBuilder();
			_sb2 = new StringBuilder();
			_tmpWordSplits = new List<WordSplit>(64);
			Censored = censored;
		}

		/// <summary>
		/// Проверяет правила на корректность, чтобы не было повторений, заменяемых букв и т.п.
		/// </summary>
		/// <returns>Сообщения об ошибках или null</returns>
		public string CheckRules() {
			var errors = new List<string>();
			var langs = Enum.GetValues(typeof (Language));
			var hash = new HashSet<string>();
			foreach (object lang in langs) {
				if (((Language) lang) == Language.Unknown) continue;
				hash.Clear();
				string[] abusive = GetAbusiveArray((Language) lang);
				foreach (var s in abusive) {
					if (s.Length < 2)
						errors.Add("\"" + s + "\" Слишком короткий шаблон");

					// проверим, чтобы шаблон не содержал заменяемые буквы
					for (int i = 0; i < s.Length; i++) {
						char ch;
						if (s[i] == ' ')
							errors.Add("\"" + s + "\" слово с пробелом");
						if (_repl.TryGetValue(s[i], out ch) && ch != s[i])
							errors.Add("\"" + s + "\" некорректный символ \"" + s[i] + "\" -> \"" + ch + "\"");
					}

					if (!hash.Add(s))
						errors.Add("\"" + s + "\" дубликат");
				}
			}
			return (errors.Count > 0 ?  string.Join("\n", errors.ToArray()) : null);
		}

		private static string[] GetAbusiveArray(Language lang) {
			switch (lang) {
				case Language.English:
					return AbusiveEn;
				case Language.Deutsch:
					return AbusiveDe;
				case Language.Spanish:
					return AbusiveEs;
				default:
					return null;
			}
		}

		/** замены символов */
		private const string Repl1 = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÄÖẞÜÑ48©&£€ƒ6#1|0®5$§7†µ×¥%АВЕЖИКМНОРСТУХЬЁавежзийкмнорстухьё";
		private const string Repl2 = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzäößüñabceeefghllorsssttuxyzabexnkmhopctyxbeabexennkmhopctyxbe";
		private static Dictionary<char, char> _repl;
		private readonly HashSet<string> _abusive;
		private readonly StringBuilder _sb, _sb2;
		private readonly List<WordSplit> _tmpWordSplits;

		private struct WordSplit
		{
			public int Begin;
			public int End;
			public string Word;
			public string Word2;
		}

		// у английского возможны окончания: s, ed, ing
		private static readonly string[] AbusiveEn = {
			"anus",
			"arse",
			"arsehole",
			"ashole",
			"ass",
			"bastard",
			//"beaver",
			"belend",
			//"bint",
			"bitch",
			"bitches",
			"bitchin",
			"blodclat",
			"blowjob",
			"blowjobs",
			"bolocks",
			"brat",
			"bulshit",
			"candyas",
			//"capon",
			"carpetmuncher",
			"clitface",
			"clunge",
			"crap",
			"cock",
			"cockburger",
			"cum",
			"cumjockey",
			"cumslut",
			"cunt",
			"cunts",
			"damn",
			"dickhead",
			"dick",
			"dildo",
			"dipshit",
			"dork",
			"douchebag",
			"dumbas",
			"fagot",
			"feck",
			"finok",
			"fuckbucket",
			"fuck",
			"fucked",
			"fucker",
			"fuckface",
			"fuckhead",
			"fucking",
			"fucknuget",
			"fucko",
			"fuckup",
			"fuckwit",
			"gash",
			"gay",
			// "goof",  "go off"
			"handjob",
			//"hole",
			"hoker",
			"jackas",
			"jade",
			"jerk",
			"jerkas",
			"jiz",
			"knob",
			"kunt",
			"loser",
			"minge",
			"moron",
			"mothafucka",
			"motherfucker",
			"munter",
			//"nancy", в т.ч. это просто имя
			"nerd",
			"noob",
			"numbnuts",
			"nigga",
			"niger",
			// "pansy", в т.ч. цветок анютины глазки
			"penis",
			"pised",
			"prat",
			"prick",
			"punani",
			"pusy",
			"quers",
			"retard",
			"sack",
			"scumbag",
			"shit",
			"shitbox",
			"shiter",
			"snatch",
			"slut",
			"sucker",
			"thundercunt",
			"twat",
			"vagina",
			"wanker",
			"weiner",
			"whore",
		};

		private static readonly string[] AbusiveDe = {
			"anschis",
			"arsch",
			"arschkriecher",
			"arschloch",
			"bescheisen",
			"beschisen",
			"fuck",
			"fick",
			"ficken",
			"fotze",
			"hure",
			"mistkerl",
			"miststueck",
			"nutte",
			"pimel",
			"scheise",
			"scheissegal",
			"scheisskerl",
			"schickse",
			"schlampe",
			"schwanzlutscher",
			"schwuchtel",
			"verarschen",
			"verfickt",
			"volscheisen",
		};

		private static readonly string[] AbusiveEs = {
			"fuck",
			"fresca",
			"furcia",
			"guara",
			"idiota",
			"imbecil",
			"perra",
			"puta",
			"puto",
			"retrasado",
			"subnormal",
			"tonta",
			"tonto",
			"zora",
		};
	}
}