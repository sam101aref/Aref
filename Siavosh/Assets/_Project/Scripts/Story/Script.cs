using Siavosh.Core;
using Siavosh.Localization;
using UnityEngine;

namespace Siavosh.Story
{
    /// <summary>The people who speak in the story.</summary>
    public static class Cast
    {
        public static readonly Speaker Naqqal = new Speaker(new LocText("نقّال", "The Storyteller"), "naqqal", Palette.GoldLight);
        public static readonly Speaker Siavosh = new Speaker(new LocText("سیاوش", "Siavosh"), "siavosh", Palette.TurquoiseLight);
        public static readonly Speaker Rostam = new Speaker(new LocText("رستم", "Rostam"), "rostam", Palette.Saffron);
        public static readonly Speaker Kavus = new Speaker(new LocText("کاووس شاه", "King Kavus"), "kavus", Palette.Rose);
        public static readonly Speaker Tus = new Speaker(new LocText("توس", "Tus"), "tus", Palette.Steel);
        public static readonly Speaker Giv = new Speaker(new LocText("گیو", "Giv"), "giv", Palette.Rose);
        public static readonly Speaker Maiden = new Speaker(new LocText("دختر بیشه", "The Maiden"), "mother", Palette.Rose);
        public static readonly Speaker Trainee = new Speaker(new LocText("نوآموز زابلی", "Zabuli trainee"), "trainee", Palette.TurquoiseLight);
        public static readonly Speaker Shepherd = new Speaker(new LocText("چوپان", "Shepherd"), null, Palette.PaperDark);
    }

    /// <summary>
    /// The story as it is told in the game, in Persian and English. The storyteller (naqqal) tells it
    /// in his coffeehouse; scenes are paintings on his curtain that come alive.
    /// Events follow the Shahnameh; small additions for play are noted in docs/siavosh/GDD.md.
    /// </summary>
    public static class Script
    {
        static Actor At(string figure, float x, float height = 0.62f, bool flip = false, float ground = 0.06f, float delay = 0f)
        {
            return Cutscene.At(figure, x, height, flip, ground, delay);
        }

        // ------------------------------------------------------------- prologue

        public static readonly Cutscene Prologue = new Cutscene("prologue", new LocText("پیش‌درآمد: زادن سیاوش", "Prologue: The Birth of Siavosh"))
            .Coffeehouse("fire", covered: true)
            .Say(Cast.Naqqal,
                "ای شنوندگان! امشب داستانی می‌گویم که هزار سال است دل‌ها را می‌سوزاند.",
                "Listen, friends! Tonight I tell a tale that has burned in hearts for a thousand years.")
            .Reveal()
            .Say(Cast.Naqqal,
                "داستان شاهزاده‌ای که از آتش گذشت و نسوخت…",
                "The tale of a prince who rode through fire and did not burn…")
            .Say(Cast.Naqqal,
                "…اما از دروغ گذر نتوانست.",
                "…but could not pass through a lie.")
            .TitleCard("logo", "پیش‌درآمد · زادن سیاوش", "Prologue · The Birth of Siavosh")
            .ScenePan("zabul", new Vector2(-0.03f, 0f), 1.1f,
                At("tus", 0.2f), At("giv", 0.34f, delay: 0.3f), At("mother", 0.74f, 0.58f, flip: true, delay: 0.8f))
            .Say(Cast.Naqqal,
                "روزی توس و گیو، دو پهلوان ایران، با سواران خود در مرز توران به شکار رفتند.",
                "One day Tus and Giv, two champions of Iran, rode out hunting with their men near the border of Turan.")
            .Say(Cast.Naqqal,
                "در بیشه‌ای به دختری رسیدند، زیبا چون ماه، که تنها نشسته بود و اشک می‌ریخت.",
                "In a thicket they came upon a maiden, lovely as the moon, sitting alone and weeping.")
            .Say(Cast.Giv,
                "ای ماه‌روی، اینجا در بیشه تنها چه می‌کنی؟",
                "Moon-faced one, what are you doing alone in this wood?")
            .Say(Cast.Maiden,
                "از خانهٔ پدر گریختم. شب مست به خانه آمد و بر من شمشیر کشید… من از تبار گرسیوزم، از خاندان فریدون.",
                "I fled my father's house. He came home drunk in the night and drew his sword on me… I am of the house of Garsivaz, of the line of Fereydun.")
            .Say(Cast.Tus,
                "من او را یافتم؛ او از آنِ من است!",
                "I found her first; she is mine!")
            .Say(Cast.Giv,
                "نه! اسب من پیش‌تر به این بیشه رسید!",
                "No! My horse reached this thicket first!")
            .Say(Cast.Naqqal,
                "کارشان چنان به تندی کشید که یکی گفت: بهتر آنکه هیچ‌کدام او را نداشته باشیم! پس داوری را نزد شاه بردند.",
                "Their quarrel grew so bitter that one of them cried: better that neither of us has her! So they took the matter to the king.")
            .ScenePan("palace", new Vector2(0.02f, 0f), 1.08f,
                At("tus", 0.14f, 0.58f), At("giv", 0.27f, 0.58f), At("kavus", 0.52f, 0.66f, flip: true), At("mother", 0.78f, 0.58f, flip: true, delay: 0.4f))
            .Say(Cast.Naqqal,
                "کاووس شاه چون دختر را دید، دل به او باخت.",
                "When King Kavus saw the maiden, he lost his heart to her.")
            .Say(Cast.Kavus,
                "پهلوانان، رنج شما بی‌پاداش نمی‌ماند؛ اما چنین ماهی سزاوار شبستان شاه است.",
                "Champions, your trouble will not go unrewarded; but such a moon belongs in the king's own hall.")
            .Say(Cast.Naqqal,
                "به هر دو اسب و تاج بخشید، و دختر بانوی شبستان شاه شد.",
                "He gave each of them horses and crowns, and the maiden became a lady of the king's house.")
            .Coffeehouse("palace")
            .Say(Cast.Naqqal,
                "نُه ماه گذشت، و پسری زاده شد که چهره‌اش چون خورشید می‌درخشید. نامش را سیاوش نهادند.",
                "Nine months passed, and a son was born whose face shone like the sun. They named him Siavosh.")
            .Say(Cast.Naqqal,
                "اخترشناسان طالعش را دیدند… و سر به زیر انداختند. ستاره‌اش درخشان بود، اما روزگارش آشفته.",
                "The astrologers read his stars… and lowered their heads. His star was bright, but his fortune troubled.")
            .ScenePan("zabul", new Vector2(0.03f, 0f), 1.06f,
                At("kavus", 0.36f, 0.64f), At("rostam", 0.62f, 0.68f, flip: true, delay: 0.3f))
            .Say(Cast.Rostam,
                "شاها، این کودک را به من بسپار. در زابلستان پرورشش می‌دهم: سواری، کمانداری، شمشیر، و راه و رسم شاهی.",
                "My king, give this child into my care. In Zabulistan I will raise him: riding, the bow, the sword, and the ways of kings.")
            .Say(Cast.Kavus,
                "جز تو کسی سزاوار این کار نیست، رستم. او را ببر.",
                "No one is more worthy of it than you, Rostam. Take him.")
            .Say(Cast.Naqqal,
                "و این‌گونه، شاهزادهٔ خردسال به سرزمین پهلوانان رفت…",
                "And so the little prince went to the land of champions…");

        // ------------------------------------------------------------- chapter 1

        public static readonly Cutscene Chapter1Intro = new Cutscene("ch1.intro", new LocText("فصل یکم: شاگرد رستم", "Chapter One: Rostam's Pupil"))
            .TitleCard("ch1", "زابلستان · سال‌ها بعد", "Zabulistan · Years later")
            .ScenePan("zabul", new Vector2(-0.02f, 0f), 1.08f, At("siavosh", 0.4f), At("rostam", 0.63f, 0.66f, flip: true))
            .Say(Cast.Naqqal,
                "سال‌ها گذشت. سیاوش در باغ‌های زابلستان بالید، و رستم هر روز چیزی تازه به او آموخت.",
                "Years went by. Siavosh grew up in the gardens of Zabulistan, and every day Rostam taught him something new.")
            .Say(Cast.Rostam,
                "امروز از نخستین گام آغاز می‌کنیم، شاهزاده. پهلوان پیش از شمشیر، راه رفتن را می‌آموزد.",
                "Today we begin with the first step, prince. A champion learns to walk before he learns the sword.")
            .Say(Cast.Siavosh,
                "هر چه بگویی، ای استاد.",
                "As you say, master.");

        public static readonly Cutscene Stage1Intro = new Cutscene("1-1.intro", new LocText("نخستین گام", "First Steps"))
            .Overlay()
            .Say(Cast.Rostam,
                "از باغ بگذر و خودت را به دروازه برسان. از جوی‌ها بپر و از صخره‌ها بالا برو.",
                "Cross the garden and reach the gate. Leap the streams and climb the rocks.")
            .Say(Cast.Siavosh,
                "و اگر چیزی در راه دیدم؟",
                "And if I find something on the way?")
            .Say(Cast.Rostam,
                "چشمانت را باز نگه دار. برگ‌هایی از نامهٔ شاهان در این باغ پراکنده است — هر کدام داستانی دارد.",
                "Keep your eyes open. Leaves from the Book of Kings are scattered through this garden — each one holds a story.");

        public static readonly Cutscene Stage2Intro = new Cutscene("1-2.intro", new LocText("شمشیر چوبی", "The Wooden Sword"))
            .Overlay()
            .Say(Cast.Rostam,
                "این شمشیر چوبی است، اما با آن چنان بجنگ که گویی پولاد است.",
                "This sword is wood, but fight with it as if it were steel.")
            .Say(Cast.Rostam,
                "نخست بر این آدمک‌ها ضربه بزن. سپس نوآموزان زابلی با تو نبرد خواهند کرد.",
                "First strike these dummies. Then the Zabuli trainees will spar with you.")
            .Say(Cast.Siavosh,
                "و اگر آن‌ها ضربه زدند؟",
                "And if they strike back?")
            .Say(Cast.Rostam,
                "سپرت را بالا ببر. پهلوان بی‌سپر، پهلوانِ یک‌روزه است.",
                "Raise your shield. A champion without a shield is a champion for a day.");

        public static readonly Cutscene Stage3Intro = new Cutscene("1-3.intro", new LocText("نیزارهای هیرمند", "Reeds of the Helmand"))
            .Overlay()
            .Say(Cast.Rostam,
                "کمان را بگیر. بادِ نیزار را بشنو، و تیر را با دمی آرام رها کن.",
                "Take the bow. Listen to the wind in the reeds, and loose the arrow on a calm breath.")
            .Say(Cast.Rostam,
                "نشانه‌ها را بزن. و گوش به زنگ باش — شنیده‌ام پیشروان تورانی از هیرمند گذشته‌اند.",
                "Strike the targets. And stay alert — I hear Turanian scouts have crossed the Helmand.");

        public static readonly Cutscene Stage4Intro = new Cutscene("1-4.intro", new LocText("کرّهٔ سیاه", "The Black Colt"))
            .Overlay()
            .Say(Cast.Rostam,
                "گله‌بانان از کرّه‌ای سیاه می‌گویند که هیچ سواری را بر پشت نمی‌پذیرد.",
                "The herdsmen speak of a black colt that will let no rider on its back.")
            .Say(Cast.Siavosh,
                "بگذار من بیازمایم.",
                "Let me try.")
            .Say(Cast.Rostam,
                "اگر رامش کردی، از آنِ توست!",
                "If you can tame him, he is yours!")
            .Say(Cast.Siavosh,
                "آرام، آرام… من از تو نمی‌ترسم، و تو هم از من نترس.",
                "Easy, easy… I am not afraid of you, and you need not fear me.");

        public static readonly Cutscene Stage4Outro = new Cutscene("1-4.outro", new LocText("شبرنگ", "Shabrang"))
            .ScenePan("zabul", new Vector2(0.03f, 0f), 1.08f, At("siavosh_riding", 0.5f, 0.5f), At("rostam", 0.82f, 0.6f, flip: true, delay: 0.5f))
            .Say(Cast.Naqqal,
                "کرّه آرام گرفت و سر بر شانهٔ سیاوش نهاد. سیاوش او را شبرنگ نامید — به رنگ شب.",
                "The colt grew calm and laid his head on Siavosh's shoulder. Siavosh named him Shabrang — the colour of night.")
            .Say(Cast.Rostam,
                "از امروز، شما دو یار جدانشدنی هستید.",
                "From this day on, you two are inseparable.");

        public static readonly Cutscene Stage5Intro = new Cutscene("1-5.intro", new LocText("شکار گور", "The Onager Hunt"))
            .Overlay()
            .Say(Cast.Rostam,
                "گوران دشت تندرو و تیزگوش‌اند. سه گور شکار کن تا شب، سفره‌مان رنگین شود.",
                "The onagers of the plain are swift and sharp-eared. Bring down three before nightfall, and our table will be full.")
            .Say(Cast.Rostam,
                "اما به کرّه‌گور تیر مینداز. شکارچی راستین، مادر و فرزند را از هم جدا نمی‌کند.",
                "But do not shoot the foal. A true hunter never parts a mother from her young.");

        public static readonly Cutscene Stage6Intro = new Cutscene("1-6.intro", new LocText("آزمون رستم", "Rostam's Trial"))
            .Overlay()
            .Say(Cast.Rostam,
                "سال‌ها آموختی. اکنون مرا بیازمای، شاهزاده. اگر سه ضربهٔ پاک به من زدی، آموزشت به پایان رسیده است.",
                "You have learned for years. Now test yourself against me, prince. Land three clean blows, and your training is complete.")
            .Say(Cast.Siavosh,
                "با تو، ای پهلوان جهان؟",
                "Against you, champion of the world?")
            .Say(Cast.Rostam,
                "گرز من سنگین است، اما کند. چشمت به دست من باشد، نه به گرزم. و هنگامی که سپر گرفته‌ام، ضربه‌ات به جایی نمی‌رسد.",
                "My mace is heavy, but slow. Watch my hands, not my mace. And while my guard is up, your blows will find nothing.");

        public static readonly Cutscene Chapter1Outro = new Cutscene("ch1.outro", new LocText("پایان فصل یکم", "End of Chapter One"))
            .ScenePan("zabul", new Vector2(-0.02f, 0f), 1.07f, At("siavosh", 0.38f), At("rostam", 0.62f, 0.66f, flip: true))
            .Say(Cast.Rostam,
                "آفرین! سه ضربه، و هر سه پاک. دیگر چیزی ندارم که به تو بیاموزم.",
                "Well done! Three blows, and every one clean. I have nothing left to teach you.")
            .Say(Cast.Rostam,
                "این را از من به یادگار داشته باش: پنجهٔ رستمی. هرگاه فرّت لبریز شد، زمین را بلرزان.",
                "Keep this as my gift: Rostam's Fist. Whenever your Farr is full, make the ground tremble.")
            .Say(Cast.Rostam,
                "اکنون بگو، شاهزاده: راهت به کجاست؟",
                "Now tell me, prince: where does your road lead?")
            .Choice(new LocText("سیاوش چه پاسخ داد؟", "What did Siavosh answer?"),
                new ChoiceOption
                {
                    Text = new LocText("«دلم هوای پدر کرده است. می‌خواهم به دربار بازگردم.»", "“My heart longs for my father. I wish to return to court.”"),
                    Faithful = true,
                    Honor = 1,
                },
                new ChoiceOption
                {
                    Text = new LocText("«همین‌جا در زابلستان می‌مانم.»", "“I will stay here in Zabulistan.”"),
                    Rewind = new LocText(
                        "سیاوش چنین نکرد. سال‌ها بود پدر را ندیده بود، و دلش هوای او را داشت.",
                        "Siavosh did no such thing. He had not seen his father in years, and his heart longed for him."),
                })
            .Say(Cast.Siavosh,
                "ای استاد، سال‌هاست پدرم را ندیده‌ام. می‌خواهم به دربار بازگردم.",
                "Master, I have not seen my father in years. I wish to return to court.")
            .Say(Cast.Rostam,
                "پس آماده شو. با شکوهی شاهانه به دربار کاووس می‌رویم.",
                "Then make ready. We ride to the court of Kavus in royal splendour.")
            .ScenePan("palace", new Vector2(0.02f, 0f), 1.08f, At("siavosh", 0.36f), At("kavus", 0.58f, 0.66f, flip: true), At("rostam", 0.16f, 0.66f))
            .Say(Cast.Naqqal,
                "کاووس چون پسر را دید، او را در آغوش گرفت و بر سر و رویش بوسه زد. شهر هفت روز جشن گرفت.",
                "When Kavus saw his son he took him in his arms and kissed his face. The city feasted for seven days.")
            .ScenePan("palace", new Vector2(-0.04f, 0f), 1.14f, At("siavosh", 0.3f), At("sudabeh", 0.78f, 0.62f, flip: true, delay: 0.8f))
            .Say(Cast.Naqqal,
                "اما در آن جشن، چشمی دیگر نیز به سیاوش دوخته شده بود… چشم سودابه، بانوی شبستان شاه.",
                "But at that feast, another pair of eyes was fixed on Siavosh… the eyes of Sudabeh, queen of the king's house.")
            .Coffeehouse("palace")
            .Say(Cast.Naqqal,
                "و اینجا، ای شنوندگان، فصل نخست به سر می‌رسد. باقی داستان را… به‌زودی برایتان خواهم گفت.",
                "And here, friends, the first chapter ends. The rest of the tale… I shall tell you soon.")
            .TitleCard("end", "فصل دوم به‌زودی", "Chapter Two — coming soon");

        /// <summary>Every full cutscene, in story order, for the story book.</summary>
        public static readonly Cutscene[] Book = { Prologue, Chapter1Intro, Stage4Outro, Chapter1Outro };
    }
}
