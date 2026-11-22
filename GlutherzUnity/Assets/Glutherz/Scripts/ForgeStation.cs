namespace Glutherz
{
    /// <summary>Der Amboss im Dorf: öffnet Schmiede, Händler und Reise-Menü.</summary>
    public class ForgeStation : Interactable
    {
        public override string Prompt { get { return "[E] Schmiede benutzen"; } }

        public override void Use()
        {
            if (GameUI.I != null) GameUI.I.OpenMenu(true);
        }
    }
}
