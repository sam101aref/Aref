using IranVsTuran.Art;
using IranVsTuran.Core;
using IranVsTuran.Defs;
using IranVsTuran.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace IranVsTuran.UI
{
    /// <summary>The Armory: permanent upgrades for towers and spells, bought with gold.</summary>
    public class UpgradesScreen : ScreenBase
    {
        protected override void Build()
        {
            Background(ArtLibrary.Backdrop(Backdrop.Castle), new Color(0.5f, 0.5f, 0.55f));
            TopBar("armory.title", () => Root.ToMap());

            var area = UIKit.Rect(Rect, "List");
            area.anchorMin = new Vector2(0.5f, 0f);
            area.anchorMax = new Vector2(0.5f, 1f);
            area.sizeDelta = new Vector2(1700f, -170f);
            area.anchoredPosition = new Vector2(0f, -70f);
            var content = UIKit.ScrollColumn(area, 20f);

            RectTransform row = null;
            for (var i = 0; i < UpgradeDefs.All.Count; i++)
            {
                if (i % 2 == 0)
                {
                    row = UIKit.Rect(content, "Row");
                    var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = 20f;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = true;
                    layout.reverseArrangement = Loc.IsRtl;
                    UIKit.Size(row, 0f, 230f);
                }
                Card(row, UpgradeDefs.All[i]);
            }
        }

        static Icon IconOf(string id)
        {
            if (id.StartsWith("archer")) return Icon.Bow;
            if (id.StartsWith("barracks")) return Icon.Shield;
            if (id.StartsWith("mage")) return Icon.Flame;
            if (id.StartsWith("artillery")) return Icon.Boulder;
            if (id == UpgradeDefs.SpellArrows) return Icon.Arrow;
            if (id == UpgradeDefs.SpellReinforce) return Icon.Helmet;
            return Icon.Coin;
        }

        void Card(RectTransform row, UpgradeDef def)
        {
            var rank = Upgrades.Rank(def.id);
            var card = UIKit.Panel(row, UIKit.Parchment, "Upgrade " + def.id);
            var icon = UIKit.Icon(card.transform, IconOf(def.id), 120f);
            UIKit.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(120f, 120f));

            var name = UIKit.Label(card.transform, Loc.T(def.NameKey), 40, UIKit.Ink, TextAnchor.MiddleLeft, true, false);
            UIKit.Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(175f, -20f), new Vector2(420f, 60f));
            var desc = UIKit.Paragraph(card.transform, Loc.Get(def.DescKey), 28, UIKit.Muted, 420f, TextAnchor.UpperLeft);
            UIKit.Place(desc.rectTransform, new Vector2(0f, 1f), new Vector2(175f, -85f), desc.rectTransform.sizeDelta);

            for (var i = 0; i < def.MaxRank; i++)
            {
                var pip = UIKit.Icon(card.transform, i < rank ? Icon.Star : Icon.StarEmpty, 44f);
                UIKit.Place(pip.rectTransform, new Vector2(0f, 0f), new Vector2(175f + i * 50f, 18f), new Vector2(44f, 44f));
            }

            if (rank < def.MaxRank)
            {
                var cost = def.costs[rank];
                var buy = UIKit.Button(card.transform, Loc.Number(cost), () =>
                {
                    if (Upgrades.Buy(def))
                    {
                        Audio.AudioService.Play(Audio.Sfx.Reward, 0.8f);
                        Rebuild();
                    }
                    else
                        Root.Toast(Loc.T("shop.not_enough_gold"));
                }, Economy.Gold >= cost ? UIKit.Green : UIKit.Muted, 36, Icon.Coin);
                UIKit.Place(buy.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(230f, 96f));
            }
            else
            {
                var max = UIKit.Label(card.transform, Loc.T("armory.max"), 36, UIKit.Green, TextAnchor.MiddleCenter, true, false);
                UIKit.Place(max.rectTransform, new Vector2(1f, 0.5f), new Vector2(-25f, 0f), new Vector2(230f, 96f));
            }
        }

        public override void OnBack()
        {
            Root.ToMap();
        }
    }
}
