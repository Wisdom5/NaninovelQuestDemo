using Features.CardMatching.Scripts.UI;
using Naninovel;

namespace Features.CardMatching.Scripts.Commands
{
    [CommandAlias("startCardGame")]
    public class StartCardGameCommand : Command
    {
        public override async UniTask ExecuteAsync(AsyncToken asyncToken = default)
        {
            var uiManager = Engine.GetService<IUIManager>();
            var cardGameUI = uiManager.GetUI<CardMatchingUI>();

            if (cardGameUI != null)
            {
                await cardGameUI.ChangeVisibilityAsync(true, asyncToken: asyncToken);
                cardGameUI.StartCardGame();
            }
        }
    }
}
