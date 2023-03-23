namespace Uno.Extensions.Maui;

public sealed class MauiEmbeddingInitializationException : MauiEmbeddingException
{
	public const string ErrorMessage = "You must call UseMauiEmbedding from the Application";

	internal MauiEmbeddingInitializationException()
		: base(ErrorMessage)
	{
	}
}
