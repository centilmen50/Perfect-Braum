using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using Color = System.Drawing.Color;
using Version = System.Version;
using System.Net;
using System.Text.RegularExpressions;

namespace PerfectBraum
{
    static class Program
    {
        public static readonly String CN = "Braum";
        public static AIHeroClient Player { get { return ObjectManager.Player; } }

        public static Item Bilgewater, Randuin, QSS, Glory, FOTMountain, Mikael, Solari;
        public static Menu Menu,Combo,Auto,Draw,Update;
        public static AIHeroClient Target = null;
        public static List<string> DodgeSpells = new List<string>() { "LuxMaliceCannon", "LuxMaliceCannonMis", "EzrealtrueShotBarrage", "KatarinaR", "YasuoDashWrapper", "ViR", "NamiR", "ThreshQ", "xerathrmissilewrapper", "yasuoq3w", "UFSlash" };
        public static List<string> DangerousDodgeSpells = new List<string>() { "KatarinaR" };
        public static readonly Spell.Skillshot Q = new Spell.Skillshot(SpellSlot.Q, 1000, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 1500, 100);
        public static readonly Spell.Targeted W = new Spell.Targeted(SpellSlot.W, 650);
        public static readonly Spell.Skillshot E = new Spell.Skillshot(SpellSlot.E, 500, EloBuddy.SDK.Enumerations.SkillShotType.Cone, 250, 2000, 500);
        public static readonly Spell.Skillshot R = new Spell.Skillshot(SpellSlot.R, 1300, EloBuddy.SDK.Enumerations.SkillShotType.Linear, 250, 1300, 140);

        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoadingComplete; }

        
        static void OnLoadingComplete(EventArgs args)
        {
            if (Player.BaseSkinName != CN) { Chat.Print("Wrong Champion Please Select Champ:  " + CN + ", addon disabled please retry"); return; }


            Bilgewater = new Item(3144, 550);
            Randuin = new Item(3143, 500);
            Glory = new Item(3800);
            QSS = new Item(3140);
            FOTMountain = new Item(3401);
            Mikael = new Item(3222, 750);
            Solari = new Item(3190, 1100);


            Menu = MainMenu.AddMenu("Perfect Braum", "Perfect Braum");
            
            string slot = "";
            string champ = "";
            Combo = Menu.AddSubMenu("Combo Settings");
            Combo.Add("ComboUseQ", new CheckBox("Use Q"));
            Combo.Add("ComboUseW", new CheckBox("Use W"));
            Combo.Add("ComboUseE", new CheckBox("Use E"));
            Combo.Add("ComboUseR", new CheckBox("Use R"));


            Auto = Menu.AddSubMenu("Auto Settings");
            Auto.Add("AutoE", new CheckBox("Auto E for Dodge Spell"));
            Auto.Add("AutoR", new CheckBox("Auto R for Dodge Dangerous Spell"));
            Auto.Add("AutoMikael", new CheckBox("Auto use Mikael to Ally"));
            Auto.Add("AutoRanduin", new CheckBox("Auto use Randuin"));
            Auto.Add("AutoGlory", new CheckBox("Auto use Glory"));
            Auto.Add("AutoFOT", new CheckBox("Auto use FOT Mountain to Ally"));
            Auto.Add("AutoSolari", new CheckBox("Auto use Iron Solari"));
            Auto.Add("AutoQSS", new CheckBox("Auto use QSS"));

            foreach (string spell in DodgeSpells)
            {
                if (EntityManager.Heroes.Enemies.Where(enemy => enemy.Spellbook.Spells.Where(it => it.SData.Name == spell && (slot = it.Slot.ToString()) == it.Slot.ToString() && (champ = enemy.BaseSkinName) == enemy.BaseSkinName).Any()).Any())
                {
                    Auto.Add(spell, new CheckBox("Interrupt " + champ + slot + " ?"));
                }
            }

            Draw = Menu.AddSubMenu("Draw Settings", "DrawSettings");
            Draw.Add("DrawAA", new CheckBox("Draw AA Range"));
            Draw.Add("DrawQ", new CheckBox("Draw Q Range"));
            Draw.Add("DrawW", new CheckBox("Draw W Range"));
            Draw.Add("DrawR", new CheckBox("Draw R Range"));

            Update = Menu.AddSubMenu("Update Logs", "UpdateLogs");
            Update.AddLabel("V0.1 Shared");
            Update.AddLabel("- Using Items");

            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            AIHeroClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;

            Chat.Print("Perfect " + CN + " Loaded");
        }

        
        static void AIHeroClient_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (DodgeSpells.Any(el => el == args.SData.Name) && Menu[args.SData.Name].Cast<CheckBox>().CurrentValue)
            {
                if (E.IsReady() && Q.IsInRange(sender))
                {
                    E.Cast(sender);
                }
                
            }
            if (DangerousDodgeSpells.Any(el => el == args.SData.Name) && Menu[args.SData.Name].Cast<CheckBox>().CurrentValue)
            {
                if (R.IsReady() && R.IsInRange(sender))
                {
                    R.Cast(sender);
                }
            }
        }
        
        
        static void Drawing_OnDraw(EventArgs args)
        {
            if (!Player.IsDead)
            {
                if (Menu["DrawQ"].Cast<CheckBox>().CurrentValue)
                {
                    Drawing.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);
                }

                if (Menu["DrawW"].Cast<CheckBox>().CurrentValue)
                {
                    Drawing.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Green : Color.Red);
                }

                if (Menu["DrawR"].Cast<CheckBox>().CurrentValue)
                {
                    Drawing.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
                }

            }
            return;
        }

        
        static void Game_OnTick(EventArgs args)
        {
            var UseFOT = (Auto["AutoFOT"].Cast<CheckBox>().CurrentValue);
            var UseMikael = (Auto["AutoMikael"].Cast<CheckBox>().CurrentValue);
            if (Player.IsDead)
            { return; }

            if (Player.CountEnemiesInRange(1000) > 0)
            {
                foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
                {
                    foreach (AIHeroClient ally in EntityManager.Heroes.Allies)
                    {
                        if (ally.IsFacing(enemy) && ally.HealthPercent <= 30 && Player.Distance(ally) <= 750)
                        {

                            if (UseFOT && FOTMountain.IsReady())
                            {
                                FOTMountain.Cast(ally);
                            }

                            if (UseMikael && (ally.HasBuffOfType(BuffType.Charm) || ally.HasBuffOfType(BuffType.Fear) || ally.HasBuffOfType(BuffType.Polymorph) || ally.HasBuffOfType(BuffType.Silence) || ally.HasBuffOfType(BuffType.Sleep) || ally.HasBuffOfType(BuffType.Snare) || ally.HasBuffOfType(BuffType.Stun) || ally.HasBuffOfType(BuffType.Taunt) || ally.HasBuffOfType(BuffType.Polymorph)) && Mikael.IsReady())
                            {
                                Mikael.Cast(ally);
                            }
                        }
                    }
                }
            }

            Target = TargetSelector.GetTarget(700, DamageType.Magical);
            var useGlory = (Auto["AutoGlory"].Cast<CheckBox>().CurrentValue);
            var useRanduin = (Auto["AutoRanduin"].Cast<CheckBox>().CurrentValue);
            if (Target != null)
            {
                if (Target.IsValidTarget())
                {
                 
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Combo))
                    {
                        var level = 15 * Player.Level;
                        if (QSS.IsReady() && (Player.HasBuffOfType(BuffType.Charm) || Player.HasBuffOfType(BuffType.Blind) || Player.HasBuffOfType(BuffType.Fear) || Player.HasBuffOfType(BuffType.Polymorph) || Player.HasBuffOfType(BuffType.Silence) || Player.HasBuffOfType(BuffType.Sleep) || Player.HasBuffOfType(BuffType.Snare) || Player.HasBuffOfType(BuffType.Stun) || Player.HasBuffOfType(BuffType.Suppression) || Player.HasBuffOfType(BuffType.Taunt)))
                        {
                            QSS.Cast();
                        }

                                foreach (AIHeroClient ally in EntityManager.Heroes.Allies)
                                {
                                    if (EntityManager.Heroes.Enemies.Where(enemy => enemy != Player && enemy.Distance(Player) <= 1000).Count() > 0 && ally.Distance(Player) <= 600 && Solari.IsReady())
                                    {
                                        if (ally.HealthPercent < 50)
                                          {
                                            Solari.Cast();
                                          }
                                    }

                                }                                         
                                        if (Q.IsReady() && Target.IsValidTarget(Q.Range) && !Player.IsDashing())
                        {
                            Q.Cast(Target);
                        }

                        foreach (AIHeroClient enemy in EntityManager.Heroes.Enemies)
                        {
                            foreach (AIHeroClient ally in EntityManager.Heroes.Allies)
                            {
                                if (W.IsReady() && ally.HealthPercent <= 30 && Target.CanCast)
                                {
                                    W.Cast(ally);
                                    if (E.IsReady())
                                    {
                                        E.Cast();
                                    }
                                    
                                }
                            }

                        }
                        if (EntityManager.Heroes.Allies.Where(ally => ally != Player && ally.Distance(Player) <= 700).Count() > 0 && Glory.IsReady() && useGlory)
                        {
                                Glory.Cast();
                                if (Q.IsReady())
                                {
                                    Q.Cast(Target);
                                }

                        }

                        if (EntityManager.Heroes.Enemies.Where(enemy => enemy != Player && enemy.Distance(Player) <= 1100).Count() > 2 && R.IsReady())
                        {
                            R.Cast(Target);
                        }

                        if (Target.IsValidTarget(Bilgewater.Range) && Bilgewater.IsReady())
                        {
                            Bilgewater.Cast(Target);
                        }

                        if (useRanduin && Target.IsValidTarget(Randuin.Range) && Randuin.IsReady())
                        {
                            Randuin.Cast();
                        }

                    }
                    if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
                    {
                        if (Q.IsReady() && Target.IsValidTarget(Q.Range) && !Player.IsDashing())
                        {
                            Q.Cast(Target);
                        }
                    }
                    foreach (AIHeroClient allie in EntityManager.Heroes.Allies)
                    {
                        if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
                        {
                            if (EntityManager.Heroes.Allies.Where(ally => ally != Player && ally.Distance(Player) <= 700).Count() > 0)
                            {
                                W.Cast(allie);
                            }
                        }
                    }
                    
                }
            }

            return;
        }

   

    }
}
