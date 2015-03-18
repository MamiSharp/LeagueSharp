﻿using System;
using System.Collections.Generic;
using System.Net;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

//Credits: ♪ princer007 ♪ and ♥ MamiSharp ♥

namespace MamiAkali
{
    class Program
    {

        static readonly Obj_AI_Hero player = ObjectManager.Player;
        static readonly string localVersion = "1.0";
        static Menu menu = new Menu("MamiAkali", "Akali", true);
        static Orbwalking.Orbwalker orbwalker;

        static Spell E;
        static Spell Q;
        static Spell R;
        static Spell W;
        static SpellSlot IgniteSlot = player.GetSpellSlot("SummonerDot");
        static bool packetCast = false;

        static Obj_AI_Hero rektmate = default(Obj_AI_Hero);
        static float assignTime = 0f;

        static List<Spell> SpellList;

        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        static void OnGameLoad(EventArgs args)
        {
            if (player.ChampionName != "Akali")
                return;

            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W, 700);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 800);

            SpellList = new List<Spell>() { Q, W, E, R };

            try
            {
                LoadMenu();
            }
            catch
            {
                Game.PrintChat("TS fucked up L#, loading without TS");
                LoadMenu(false);
            }

            UpdateChecks();
            Console.WriteLine("\a \a \a");
            Drawing.OnDraw += OnDraw;
            Game.OnGameUpdate += OnUpdate;
        }

        static void OnCast(LeagueSharp.Obj_AI_Base sender, LeagueSharp.GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsEnemy) return;
            if (args.SData.Name == "TrinketTotemLvl3B" || args.SData.Name == "VisionWard" && menu.SubMenu("misc").Item("antipink").GetValue<bool>())
            {
                if (args.End.Distance(player.Position) < 1200)
                    Utility.DelayAction.Add(200, () => AntiPink(args.End));
            }
        }
        //

        static void OnUpdate(EventArgs args)
        {
            packetCast = menu.Item("packets").GetValue<bool>();
            orbwalker.SetAttack(true);
            if (menu.Item("RKillsteal").GetValue<bool>())
                foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>())
                    if (enemy.IsEnemy && Vector3.Distance(player.Position, enemy.Position) <= R.Range && player.GetSpellDamage(enemy, SpellSlot.R) > enemy.Health && ultiCount() > 0 && R.IsReady())
                        R.CastOnUnit(enemy);

            switch (orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Combo:
                    RapeTime();
                    break;

                case Orbwalking.OrbwalkingMode.Mixed:
                    if (menu.SubMenu("harass").Item("useQ").GetValue<bool>())
                        CastQ(true);
                    if (menu.SubMenu("harass").Item("useE").GetValue<bool>())
                        CastE(true);
                    break;

                case Orbwalking.OrbwalkingMode.LaneClear:
                    if (menu.SubMenu("laneclear").Item("useQ").GetValue<bool>())
                        CastQ(false);
                    if (menu.SubMenu("laneclear").Item("useE").GetValue<bool>())
                        CastE(false);
                    break;
            }
            if (menu.SubMenu("misc").Item("escape").GetValue<KeyBind>().Active) Escape();
        }

        private static void OnDraw(EventArgs args)
        {
            if (menu.SubMenu("misc").Item("escape").GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(Game.CursorPos, 200, W.IsReady() ? Color.Blue : Color.Red, 3);
                Render.Circle.DrawCircle(player.Position, R.Range, menu.Item("Rrange").GetValue<Circle>().Color, 13);
            }
            foreach (var spell in SpellList)
            {
                var menuItem = menu.Item(spell.Slot + "range").GetValue<Circle>();
                if (menuItem.Active)
                    Render.Circle.DrawCircle(player.Position, spell.Range, menuItem.Color);
            }
            if (menu.SubMenu("drawings").Item("RAPE").GetValue<bool>() && rektmate != default(Obj_AI_Hero))
                Render.Circle.DrawCircle(rektmate.Position, 70, Color.ForestGreen, 8);
        }

        static void AntiPink(Vector3 position)
        {
            float pd = player.Distance(position);
            foreach (var item in player.InventoryItems)
                switch (item.Name)
                {
                    case "TrinketSweeperLvl1":
                        if (pd < 800)
                            player.Spellbook.CastSpell(item.SpellSlot, V2E(player.Position, position, 400).To3D());
                        break;
                    case "TrinketSweeperLvl2":
                        if (pd < 1200)
                            player.Spellbook.CastSpell(item.SpellSlot, V2E(player.Position, position, 600).To3D());
                        break;
                    case "TrinketSweeperLvl3":
                        if (pd < 1200)
                            player.Spellbook.CastSpell(item.SpellSlot, V2E(player.Position, position, 600).To3D());
                        break;
                }
        }

        private static void CastQ(bool mode)
        {
            if (!Q.IsReady()) return;
            if (mode)
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (!target.IsValidTarget(Q.Range)) return;
                Q.Cast(target);
            }
            else
            {
                foreach (
                    Obj_AI_Base minion in
                        MinionManager.GetMinions(player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy,
                            MinionOrderTypes.Health))
                    if (hasBuff(minion, "AkaliMota") &&
                        Orbwalking.GetRealAutoAttackRange(player) >= Vector3.Distance(player.Position, minion.Position))
                        orbwalker.ForceTarget(minion);

                foreach (
                    Obj_AI_Base minion in
                        MinionManager.GetMinions(player.Position, Q.Range, MinionTypes.All, MinionTeam.Enemy,
                            MinionOrderTypes.Health))
                    if (
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(E.Delay + (Vector3.Distance(player.Position, minion.Position) / E.Speed)) * 1000) <
                        player.GetSpellDamage(minion, SpellSlot.Q) &&
                        HealthPrediction.GetHealthPrediction(minion,
                            (int)(E.Delay + (Vector3.Distance(player.Position, minion.Position) / E.Speed)) * 1000) > 0 &&
                        Vector3.Distance(player.Position, minion.Position) > Orbwalking.GetRealAutoAttackRange(player))
                        Q.Cast(minion);

                foreach (Obj_AI_Base minion in MinionManager.GetMinions(player.ServerPosition, Q.Range,
                    MinionTypes.All,
                    MinionTeam.Neutral, MinionOrderTypes.MaxHealth))
                    if (Vector3.Distance(player.Position, minion.Position) <= Q.Range)
                        Q.Cast(minion);


            }
        }

        static void CastE(bool mode)
        {
            if (!E.IsReady()) return;
            if (mode)
            {
                Obj_AI_Hero target = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (target == null || !target.IsValidTarget(E.Range)) return;
                if (hasBuff(target, "AkaliMota") && !E.IsReady() && Orbwalking.GetRealAutoAttackRange(player) >= Vector3.Distance(player.Position, target.Position))
                    orbwalker.ForceTarget(target);
                else
                    E.Cast();
            }
            else
            {   
                if (MinionManager.GetMinions(player.Position, E.Range, MinionTypes.All, MinionTeam.Enemy).Count >= menu.SubMenu("laneclear").Item("hitCounter").GetValue<Slider>().Value) E.Cast();
                foreach (Obj_AI_Base minion in MinionManager.GetMinions(player.ServerPosition, Q.Range,
                      MinionTypes.All,
                      MinionTeam.Neutral, MinionOrderTypes.MaxHealth))
                    if (Vector3.Distance(player.Position, minion.Position) <= E.Range)
                        E.Cast();
            }
        }

        static void RapeTime()
        {
            Obj_AI_Hero possibleVictim = TargetSelector.GetTarget(R.Range * 2 + Orbwalking.GetRealAutoAttackRange(player), TargetSelector.DamageType.Magical);
            try
            {
                if (rektmate.IsDead || Game.Time - assignTime > 1.5)
                {
                    rektmate = default(Obj_AI_Hero);
                }
            }
            catch { }
            try
            {
                if (rektmate == default(Obj_AI_Hero) && IsRapeble(possibleVictim) > possibleVictim.Health)
                {
                    rektmate = possibleVictim;
                    assignTime = Game.Time;
                }
            }
            catch { }
            if (rektmate != default(Obj_AI_Hero))
            {
                if (Vector3.Distance(player.Position, rektmate.Position) < R.Range * 2 + Orbwalking.GetRealAutoAttackRange(player) && Vector3.Distance(player.Position, rektmate.Position) > Q.Range)
                    CastR(rektmate.Position);
                else if (Vector3.Distance(player.Position, rektmate.Position) < Q.Range)
                    RaperinoCasterino(rektmate);
                else rektmate = default(Obj_AI_Hero);
            }
            else
            {
                orbwalker.SetAttack(!Q.IsReady() && !E.IsReady());
                if (menu.SubMenu("combo").Item("useQ").GetValue<bool>())
                    CastQ(true);
                if (menu.SubMenu("combo").Item("useE").GetValue<bool>())
                    CastE(true);
                if (menu.SubMenu("combo").Item("useW").GetValue<bool>())
                    CastW();
                if (menu.SubMenu("combo").Item("useR").GetValue<bool>())
                {
                    Obj_AI_Hero target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                    if ((target.IsValidTarget(R.Range) && Vector3.Distance(player.Position, target.Position) > Orbwalking.GetRealAutoAttackRange(player)) || R.IsKillable(target))
                        R.Cast(target, packetCast);
                }
            }
        }

        static void CastW()
        {
            
            byte enemiesAround = 0;
            foreach (Obj_AI_Hero enemy in ObjectManager.Get<Obj_AI_Hero>())
                if (enemy.IsEnemy && Vector3.Distance(player.Position, enemy.Position) < 400) enemiesAround++;
            if (menu.Item("PanicW").GetValue<Slider>().Value > enemiesAround && menu.Item("PanicWN").GetValue<Slider>().Value < (int)(player.Health / player.MaxHealth * 100))
                return;
            W.Cast(player.Position, packetCast);
        }

        static void RaperinoCasterino(Obj_AI_Hero victim)
        {
            orbwalker.SetAttack(!Q.IsReady() && !E.IsReady() && Vector3.Distance(player.Position, victim.Position) < 800f);
            orbwalker.ForceTarget(victim);
            foreach (var item in player.InventoryItems)
                switch ((int)item.Id)
                {
                    case 3144:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) player.Spellbook.CastSpell(item.SpellSlot, victim);
                        break;
                    case 3146:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) player.Spellbook.CastSpell(item.SpellSlot, victim);
                        break;
                    case 3128:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready) player.Spellbook.CastSpell(item.SpellSlot, victim);
                        break;
                }
            if (Q.IsReady() && Q.IsInRange(victim.Position) && !hasBuff(victim, "AkaliMota")) Q.Cast(victim, packetCast);
            if (E.IsReady() && E.IsInRange(victim.Position)) E.Cast();
            if (W.IsReady() && W.IsInRange(victim.Position) && !(hasBuff(victim, "AkaliMota") && Vector3.Distance(player.Position, victim.Position) > Orbwalking.GetRealAutoAttackRange(player))) W.Cast(V2E(player.Position, victim.Position, Vector3.Distance(player.Position, victim.Position) + W.Width * 2 - 20), packetCast);
            if (R.IsReady() && R.IsInRange(victim.Position)) R.Cast(victim, packetCast);
            if (IgniteSlot != SpellSlot.Unknown && player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready) player.Spellbook.CastSpell(IgniteSlot, victim);
        }

        static double IsRapeble(Obj_AI_Hero victim)
        {
            int UC = ultiCount();
            int jumpCount = (UC - (int)(victim.Distance(player.Position) / R.Range));
            double comboDamage = 0d;
            if (Q.IsReady()) comboDamage += player.GetSpellDamage(victim, SpellSlot.Q) + player.CalcDamage(victim, Damage.DamageType.Magical, (45 + 35 * Q.Level + 0.5 * player.FlatMagicDamageMod));
            if (E.IsReady()) comboDamage += player.GetSpellDamage(victim, SpellSlot.E);

            if (hasBuff(victim, "AkaliMota")) comboDamage += player.CalcDamage(victim, Damage.DamageType.Magical, (45 + 35 * Q.Level + 0.5 * player.FlatMagicDamageMod));
            
            comboDamage += player.CalcDamage(victim, Damage.DamageType.Magical, CalcPassiveDmg());
            comboDamage += player.CalcDamage(victim, Damage.DamageType.Magical, CalcItemsDmg(victim));

            foreach (var item in player.InventoryItems)
                if ((int)item.Id == 3128)
                    if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                        comboDamage *= 1.2;
            if (hasBuff(victim, "deathfiregraspspell")) comboDamage *= 1.2;

            if (UC > 0) comboDamage += jumpCount > 0 ? player.GetSpellDamage(victim, SpellSlot.R) * jumpCount : player.GetSpellDamage(victim, SpellSlot.R);
            if (IgniteSlot != SpellSlot.Unknown && player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                comboDamage += ObjectManager.Player.GetSummonerSpellDamage(victim, Damage.SummonerSpell.Ignite);
            return comboDamage;
        }

        static double CalcPassiveDmg()
        {
            return (0.06 + 0.01 * (player.FlatMagicDamageMod / 6)) * (player.FlatPhysicalDamageMod + player.BaseAttackDamage);
        }

        static double CalcItemsDmg(Obj_AI_Hero victim)
        {
            double result = 0d;
            foreach (var item in player.InventoryItems)
                switch ((int)item.Id)
                {
                    case 3100: 
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += player.BaseAttackDamage * 0.75 + player.FlatMagicDamageMod * 0.5;
                        break;
                    case 3057:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += player.BaseAttackDamage;
                        break;
                    case 3144:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += 100d;
                        break;
                    case 3146:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += 150d + player.FlatMagicDamageMod * 0.4;
                        break;
                    case 3128:
                        if (player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                            result += victim.MaxHealth * 0.15;
                        break;
                }

            return result;
        }

        static void Escape()
        {
            Vector3 cursorPos = Game.CursorPos;
            Vector2 pos = V2E(player.Position, cursorPos, R.Range);
            Vector2 pass = V2E(player.Position, cursorPos, 120);
            player.IssueOrder(GameObjectOrder.MoveTo, new Vector3(pass.X, pass.Y, 0));
            if (menu.SubMenu("misc").Item("RCounter").GetValue<Slider>().Value > ultiCount()) return;
            if (!IsWall(pos) && IsPassWall(player.Position, pos.To3D()) && MinionManager.GetMinions(cursorPos, 300, MinionTypes.All, MinionTeam.NotAlly).Count < 1)
                if (W.IsReady()) W.Cast(V2E(player.Position, cursorPos, W.Range));
            CastR(cursorPos, true);
        }

        static void CastR(Vector3 position, bool mouseJump = false)
        {
            Obj_AI_Base target = player;
            foreach (Obj_AI_Base minion in ObjectManager.Get<Obj_AI_Base>())
                if (minion.IsValidTarget(R.Range, true) && player.Distance(position, true) > minion.Distance(position, true) && minion.Distance(position, true) < target.Distance(position, true))
                    if (mouseJump)
                    {
                        if (minion.Distance(position) < 200)
                            target = minion;
                    }
                    else
                        target = minion;
            if (R.IsReady() && R.IsInRange(target.Position) && !target.IsMe)
                if (mouseJump && target.Distance(position) < 200)
                    R.CastOnUnit(target, packetCast);
                else if (player.Distance(position, true) > target.Distance(position, true))
                    R.CastOnUnit(target, packetCast);

        }

        static bool IsPassWall(Vector3 start, Vector3 end)
        {
            double count = Vector3.Distance(start, end);
            for (uint i = 0; i <= count; i += 10)
            {
                Vector2 pos = V2E(start, end, i);
                if (IsWall(pos)) return true;
            }
            return false;
        }

        static int ultiCount()
        {
            foreach (BuffInstance buff in player.Buffs)
                if (buff.Name == "AkaliShadowDance")
                    return buff.Count;
            return 0;
        }

        static bool IsWall(Vector2 pos)
        {
            return (NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Wall ||
                    NavMesh.GetCollisionFlags(pos.X, pos.Y) == CollisionFlags.Building);
        }

        static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.To2D() + distance * Vector3.Normalize(direction - from).To2D();
        }
        static bool hasBuff(Obj_AI_Base target, string buffName)
        {
            foreach (BuffInstance buff in target.Buffs)
                if (buff.Name == buffName) return true;
            return false;
        }

        static bool ableToGapclose(Obj_AI_Base target)
        {

            return false;
        }

        static void LoadMenu(bool mode = true)
        {
            if (mode)
            {
                Menu targetSelector = new Menu("♥ Escolher Foco ♥", "ts");
                TargetSelector.AddToMenu(targetSelector);
                menu.AddSubMenu(targetSelector);
            }

            Menu SOW = new Menu("Orbwalker", "orbwalker");
            orbwalker = new Orbwalking.Orbwalker(SOW);
            menu.AddSubMenu(SOW);

            menu.AddSubMenu(new Menu("♥ Opções do Combo ♥", "combo"));
            menu.SubMenu("combo").AddItem(new MenuItem("useQ", "Usar Q no combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useW", "Usar W no combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useE", "Usar E no combo").SetValue(true));
            menu.SubMenu("combo").AddItem(new MenuItem("useR", "Usar R no combo").SetValue(true));

            menu.AddSubMenu(new Menu("♥ Opções de Harass ♥", "harass"));
            menu.SubMenu("harass").AddItem(new MenuItem("useQ", "Usar Q no harass").SetValue(false));
            menu.SubMenu("harass").AddItem(new MenuItem("useE", "Usar E no harass").SetValue(true));

            menu.AddSubMenu(new Menu("♥ Limpar Lane/Farmar ♥", "laneclear"));
            menu.SubMenu("laneclear").AddItem(new MenuItem("useQ", "Usar Q no lasthit").SetValue(true));
            menu.SubMenu("laneclear").AddItem(new MenuItem("useE", "Usar E para limpar a lane").SetValue(true));
            menu.SubMenu("laneclear").AddItem(new MenuItem("hitCounter", "Usar E se atacado").SetValue(new Slider(3, 1, 6)));

            menu.AddSubMenu(new Menu("♥ Diversos ♥", "misc"));
            menu.SubMenu("misc").AddItem(new MenuItem("0", "                       Ulti:"));
            menu.SubMenu("misc").AddItem(new MenuItem("escape", "Botão de Fuga").SetValue(new KeyBind('G', KeyBindType.Press)));
            menu.SubMenu("misc").AddItem(new MenuItem("RCounter", "Não usar R para Fugir<").SetValue(new Slider(1, 1, 3)));
            menu.SubMenu("misc").AddItem(new MenuItem("RKillsteal", "Sempre tentar dar KS com o R").SetValue(false));
            menu.SubMenu("misc").AddItem(new MenuItem("1", "                      Pânico W:"));
            menu.SubMenu("misc").AddItem(new MenuItem("PanicW", "Cercado por # inimigos").SetValue(new Slider(1, 1, 5)));
            menu.SubMenu("misc").AddItem(new MenuItem("PanicWN", "Vida abaixo de % ").SetValue(new Slider(25, 0, 100)));
            menu.SubMenu("misc").AddItem(new MenuItem("2", "                      Outros:"));
            menu.SubMenu("misc").AddItem(new MenuItem("packets", "Usar packets").SetValue(true));
           
            var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Mostrar Dano após o combo").SetValue(true);
            Utility.HpBarDamageIndicator.DamageToUnit += hero => (float)IsRapeble(hero);
            Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
            dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
            {
                Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            };

            Menu drawings = new Menu("♥ Exibir ♥", "drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("Qrange", "Alcance do Q").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("Wrange", "Alcance do W").SetValue(new Circle(true, Color.FromArgb(150, Color.IndianRed))));
            drawings.AddItem(new MenuItem("Erange", "Alcance do E").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("Rrange", "Alcance do R").SetValue(new Circle(false, Color.FromArgb(150, Color.DarkRed))));
            drawings.AddItem(new MenuItem("RAPE", "Mostrar Instakill").SetValue<bool>(true));
            drawings.AddItem(dmgAfterComboItem);

            menu.AddToMainMenu();
        }

        static void UpdateChecks()
        {
            Game.PrintChat("<font color='#ffb2b2'>♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥ ♥</font>");
            WebClient client = new WebClient();
            string version = client.DownloadString("https://github.com/MamiSharp/LeagueSharp/MamiAkali/version");
            if (version.Remove(4).Equals(localVersion))
                Game.PrintChat("<font color='#ffb2b2'>♥ MamiAkali está Atualizado! ♥</font>");
            else
                Game.PrintChat("<font color='#ffb2b2'>♥ MamiAkali não está Atualizado. Atualize as suas Assemblies ♥</font>");

            Utility.DelayAction.Add(300, () => Game.PrintChat("<font color='#ffb2b2'>♥ MamiSharp Carregando você! ♥</font>"));
            Utility.DelayAction.Add(100, () => Game.PrintChat("<font color='#ffb2b2'>♪ MamiAkali Carregado. Doações Paypal:MamiSharp@asia.com ♪</font>"));
        }
    }
}