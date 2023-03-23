namespace Uno.Extensions.Maui.Internals;

internal class EmbeddingApp : Microsoft.Maui.Controls.Application
{
	public EmbeddingApp(Application app)
	{
		var resources = app.Resources.ToMauiResources(); ;
		if(resources.Keys.Any())
		{
			Resources.MergedDictionaries.Add(resources);
		}
	}
}
