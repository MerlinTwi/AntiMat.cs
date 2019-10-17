namespace Client.Chat
{

	public interface IAntimat {

		/// <summary>
		/// По какому шаблону была последняя замена (используется при анализе ложных срабатываний)
		/// </summary>
		string LastUsedPattern { get; }

		/// <summary>
		/// Заменает в тексте мат и оскобления
		/// </summary>
		/// <param name="text">обрабатываемый текст</param>
		/// <returns>обработанный текст</returns>
		string RemoveMat(string text);

		/// <summary>
		/// Проверяет правила на корректность, чтобы не было повторений, заменяемых букв и т.п.
		/// </summary>
		/// <returns>Сообщения об ошибках или null</returns>
		string CheckRules();
	}
}