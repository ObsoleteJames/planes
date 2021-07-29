
using Sandbox.UI;

class GameHud : Sandbox.HudEntity<RootPanel>
{
	public GameHud()
	{
		if (IsClient)
		{
			RootPanel.SetTemplate( "/gamehud.html" );
		}
	}
}
