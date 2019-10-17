using System;
using System.IO;
using System.Text;
using Client.Chat;

namespace AntiMat
{
	class Program
	{

		private const string TextFolder = "../../../text_for_test/";
		private const string AbusiveRuFileName = "abusive_ru.txt";
		private const string AbusiveEnFileName = "abusive_en.txt";
		private const string NotAbusiveRuFileName= "not_abusive_ru.txt";
		private const string NotAbusiveEnFileName = "not_abusive_en.txt";
		private const string MasterIMargaritaFileName= "Master_i_Margarita.txt";
		private const string WikiWordsRuFileName= "wiki_freq_ru.txt";
		private const string WikiWordsEnFileName= "wiki_freq_en.txt";
		private const string AllRuWordsFileName = "all_words_ru.txt";
		private const string AllEnWordsFileName = "all_words_en.txt";

		private static readonly string[] PredlogsRu =
		{
			"в", "до", "из", "к", "на", "не", "нет", "да", "же", "ее", "по", "о", "от", "он", "ох", "ах", "с", "у",
			"за", "об", "как", "но"
		};
		private static readonly string[] PredlogsEn =
		{
			"a", "an", "the",
			"at", "in", "about", "before", "against", "for", "to", "by", "from", "of", "since", "with",
			"on", "off", "up", "out", "ago", "onto", "over", "past", "through", "under", "till", "untill",
		};

		/**
		 * Встречаются некорректные варианты слов с предлогами на которые срабатывает фильтр
		 * т.к. предлоги тупо добавляются все подряд без каких-либо правил
		 */
		private static readonly string[] WrongPredlogs = {
			"<С ЦУКАТ", "<С ЦУКАНИЕ>", "<НА ХЕРШИ-КО", "<ПО ХЕРТЕЛЬ>",
			"<НА ХЕРСОН", "<НА ХЕРИК>", "<ПО ХЕРЕС", "<ПО ХЕРДЕЛЬ>",
			"<ОХ УЯСН", "<ОХ УЯРЦ", "<ОХ УЯРЕЦ>", "<С УЧЕН", "<С УКИСАНИЕ>",
			"<С УКИПАНИЕ>", "<ОХ УЙМ", "<ОХ УЙГ", "<ОХ УЙТ", "<ОХ УИН", "<ОХ УИМ", "<ОХ УИЛ", "<ОХ УИК",
			"<ОХ УЕСТ", "<ОХ УЕЛО>", "<ОХ УЕЛИ>", "<ОХ УЕЛА>", "<АХ УЕЛ>", "<ОХ УЕЗ", "<АХ УЕВ>",
			"<ОХ УЕМ>", "<ОХ УЁМ>", "<С У-КАМЕНОГОРСК", "<ОТ СОСИТЕ>", "<ОТ СОСАЛ", "<С РУЛЬ>",
			"<У РОД>", "<С РАКИЯ>", "<С РАКИ>", "<С РАКА>", "<С ОСИ>", "<ЗА ЛУП",
			"ИХ У ИХ", "<У Е-БИЗНЕ", "<С УЧЁНОСТЬ>",
			"<ОБ ОСРАМ", "<С РАНЬ>", "<ОХ РЕНЕТ>", "<АХ РЕНЕТ>",
			"<MOOR> on", "<AN> U.S."
		};

		static void Main(string[] args) {
			Console.WriteLine("Начинаем проверку Antimat, это займет несколько минут...");
			try {
				var ru = new AntimatRu(null);
				var en = new AntimatLang(AntimatLang.Language.English, null);

				// проверка шаблонов
				TestRules(ru);
				TestRules(en);

				// проверка матерных слов
				TestAbusive(ru, Path.Combine(TextFolder, AbusiveRuFileName));
				TestAbusive(en, Path.Combine(TextFolder, AbusiveEnFileName));

				// Загружаем файл с не матерными словами где возможны ложные срабатывания.
				TestNotAbusive(ru, Path.Combine(TextFolder, NotAbusiveRuFileName));
				TestNotAbusive(en, Path.Combine(TextFolder, NotAbusiveEnFileName));

				// проверка часто встречающихся слов на википедии
				TestNotAbusive(ru, Path.Combine(TextFolder, WikiWordsRuFileName));
				TestNotAbusive(en, Path.Combine(TextFolder, WikiWordsEnFileName));

				// проверка всех русских слов в комбинации с предлогами
				// именно в таком варианте больше всего ложных срабатываний
				Console.WriteLine("Проверим отдельные слова в комбинации с различными предлогами. Это надолго!");
				Console.WriteLine("Английские");
				TestNotAbusiveWithSuffix(en, Path.Combine(TextFolder, AllEnWordsFileName), PredlogsEn);
				Console.WriteLine("Русские");
				TestNotAbusiveWithSuffix(ru, Path.Combine(TextFolder, AllRuWordsFileName), PredlogsRu);

				// проверка книги.
				// в оригинальном тексте книги содержались оскорбительные слова
				// пришлось их заменить на *** :)
				TestNotAbusive(ru, Path.Combine(TextFolder, MasterIMargaritaFileName));

				Console.WriteLine("Проверка законцена.");
			} catch (Exception ex) {
				Console.WriteLine("ОШИБКА: " + ex);
			}

			Console.WriteLine("\nНажмите любую клавишу...");
			Console.ReadKey();
		}
		
		/** Проверка шаблонов */
		private static void TestRules(IAntimat antimat) {
			Console.WriteLine("Проверка правил.");
			var s = antimat.CheckRules();
			if (!string.IsNullOrEmpty(s))
				throw new Exception("Ошибки в правилах:\n" + s);
			Console.WriteLine("Все правила корректны.");
		}
		
		/** проверка матерных слов, все строки должны быть заменены */
		private static void TestAbusive(IAntimat antimat, string file) {
			Console.WriteLine("Загрузка матерных слов из файла: " + file);
			var s = File.ReadAllText(file);
			int errors = 0;
			var words = s.Replace("\r", "").Split('\n');
			foreach (var w in words) {
				if (string.IsNullOrEmpty(w)) continue;
				if (w == antimat.RemoveMat(w)) {
					Console.WriteLine("ОШИБКА. Не произошло замены матерных слов: " + w);
					if (++errors > 10) break;
				}
				// else Console.WriteLine(w + " ===> " + w2);
			}
			if (errors > 0) {
				throw new Exception("Проверка прервана. Исправьте правила.");
			}
			Console.WriteLine("Успешно закончена проверка матерных слов из файла: " + file);
		}
		
		/** проверка не матерных слов. замен быть не должно */
		private static void TestNotAbusive(IAntimat antimat, string file) {
			Console.WriteLine("Загрузка НЕ матерных слов из файла: " + file);
			var s = File.ReadAllText(file);
			int errors = 0;
			var words = s.Replace("\r", "").Split('\n');
			var sb = new StringBuilder();
			foreach (var w in words) {
				if (string.IsNullOrEmpty(w)) continue;
				sb.Append(" ").Append(w);
				var w2 = antimat.RemoveMat(w);
				if (w != w2) {
					Console.WriteLine("ОШИБКА. Ложное срабатывание правила '" + antimat.LastUsedPattern + "' в тексте " + w2);
					sb.Length = 0;
					if (++errors > 10) break;
				}

				if (sb.Length > 2000) {
					var long1 = sb.ToString();
					var long2 = antimat.RemoveMat(long1);
					if (long1 != long2) {
						Console.WriteLine("ОШИБКА. Ложное срабатывание правила '" + antimat.LastUsedPattern + "' в полном тексте " + long2);
						if (++errors > 10) break;
					}
					sb.Length = 0;
				}
			}
			if (sb.Length > 0) {
				var long1 = sb.ToString();
				var long2 = antimat.RemoveMat(long1);
				if (long1 != long2) {
					Console.WriteLine("ОШИБКА. Ложное срабатывание правила '" + antimat.LastUsedPattern + "' в полном тексте " + long2);
					++errors;
				}
			}
			if (errors > 0) {
				throw new Exception("Проверка прервана. Исправьте правила.");
			}

			Console.WriteLine("Успешно закончена проверка НЕ матерных слов из файла: " + file);
		}

		
		/** проверка отдельных не матерных слов с приставками и суффиксами. замен быть не должно */
		private static void TestNotAbusiveWithSuffix(IAntimat antimat, string file, string[] predlogs) {
			Console.WriteLine("Загрузка слов из файла: " + file);
			var s = File.ReadAllText(file);

			int errors = 0;
			var words = s.Replace("\r", "").Split('\n');
			Console.Write("0%...");
			int lastPercent = 0;
			for (int i = 0; i < words.Length; i++) {
				var w = words[i];
				if (string.IsNullOrEmpty(w)) continue;

				int percent = i*100/words.Length;
				if (percent > lastPercent) {
					lastPercent = percent;
					Console.Write(" " + percent);
				}

				// комбинируем слово со всевозможными предлогами
				var withPredlogs = w + " " + string.Join(" " + w + " ", predlogs) + " " + w;
				var w2 = antimat.RemoveMat(withPredlogs);
				if (withPredlogs != w2) {
					// возможно это некорректная комбинация с предлогом
					var itError = true;
					foreach (var wp in WrongPredlogs)
						if (w2.IndexOf(wp, StringComparison.Ordinal) >= 0) {
							itError = false;
							break;
						}
					if (itError) {
						Console.WriteLine("\nОШИБКА. Ложное срабатывание правила '" + antimat.LastUsedPattern + "' в тексте " +
										  w2);
						if (++errors > 10) break;
					}
				}
			}
			if (errors > 0)
				throw new Exception("Проверка прервана. Исправьте правила.");
			Console.WriteLine("");
			Console.WriteLine("Успешно закончена проверка слов в комбинации с предлогами");
		}
		
	}
}
