﻿using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using ECommons.ImGuiMethods;
using GambaGames.Games;

namespace GambaGames.Windows;

public class MainWindow : Window
{
    private string _imagePath;
    private IPartyList _party;
    private IClientState _clientState;
    private Plugin _plugin;
        
    private bool ShowGame = true;
    private static int Decks = 2;
    private string? DealerName;
    private string? GameResults;
    private bool GameInProgress;
    private bool GameIsOver;
    private bool HideCard = true;
    private static string[] ChatTypes;
    private static int SelectedChatType;
    private static Configuration Configuration;
    private static Dictionary<string, bool> PartyPlaying;
        
    private List<string> Players = new();
    private List<string> NonPartyplayers = new();
        
    private static string[] PlayersHands;
    private static string[] PlayersChoices;
    private static bool[] CanSurrender;
    private List<string> HasSurrendered = new();
    private static Dictionary<string, string> PlayersBets;
    private Dictionary<string, int> SplitHandsResults = new();
        
    public OpenWindow OpenWindow { get; private set; } = OpenWindow.Overview;
        
    public MainWindow(Plugin plugin, string imagePath) : base(
        "GambaGames", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        _plugin = plugin;

        _imagePath = imagePath;
        
        _party = Plugin.PartyList;
        _clientState = Plugin.Client;
            
        Deck.ClearDeck();
        Deck.CreateDeck(Decks);
        Deck.Shuffle();

        DealerName = _clientState.LocalPlayer?.Name.TextValue;
    }

    public override void Draw()
    {
        DealerName = _clientState.LocalPlayer?.Name.TextValue;
        var logo = _plugin.TextureProvider.GetFromFile(_imagePath).GetWrapOrDefault();
        try
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(5f.Scale(), 0));
            if (ImGui.BeginTable($"GamblingTableContainer", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);

                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();

                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                    
                if (ImGui.BeginChild($"GamblingLeftBar", regionSize with { Y = ImGui.GetContentRegionAvail().Y }, false, ImGuiWindowFlags.NoDecoration))
                {
                    ImGuiEx.LineCentered("GamblingLogo", () =>
                    {
                        if (logo != null)
                        {
                            ImGui.Image(logo.ImGuiHandle, new Vector2(125f.Scale(), 125f.Scale()));
                        }
                    });
                        
                    ImGui.Spacing();
                    ImGui.Separator();

                    if (!GameInProgress)
                    {
                        if (ImGui.Selectable("Overview", OpenWindow == OpenWindow.Overview))
                        {
                            OpenWindow = OpenWindow.Overview;
                        }

                        if (ImGui.Selectable("BlackJack", OpenWindow == OpenWindow.Blackjack))
                        {
                            // Create data structures for storing game info
                            PartyPlaying = new Dictionary<string, bool>();
                            PlayersBets = new Dictionary<string, string>();
                            PlayersHands = new String[16]
                                { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                            PlayersChoices = new String[16]
                                { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                            ChatTypes = new String[] { "p", "s", "shout" };
                            CanSurrender = new bool[16]
                            {
                                true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                                true, true
                            };
                            OpenWindow = OpenWindow.Blackjack;
                            GameIsOver = false;
                            GameResults = "";
                            HideCard = true;
                        }

                        if (ImGui.Selectable("Deathroll", OpenWindow == OpenWindow.DeathRoll))
                        {
                            OpenWindow = OpenWindow.DeathRoll;
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable("Overview", OpenWindow == OpenWindow.Overview, ImGuiSelectableFlags.Disabled))
                        {
                            OpenWindow = OpenWindow.Overview;
                        }

                        if (ImGui.Selectable("BlackJack", OpenWindow == OpenWindow.Blackjack, ImGuiSelectableFlags.Disabled))
                        {
                            // Create data structures for storing game info
                            PartyPlaying = new Dictionary<string, bool>();
                            PlayersBets = new Dictionary<string, string>();
                            PlayersHands = new String[16]
                                { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                            PlayersChoices = new String[16]
                                { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
                            ChatTypes = new String[] { "p", "s", "shout" };
                            CanSurrender = new bool[16]
                            {
                                true, true, true, true, true, true, true, true, true, true, true, true, true, true,
                                true, true
                            };
                            OpenWindow = OpenWindow.Blackjack;
                            GameIsOver = false;
                            GameResults = "";
                            HideCard = true;
                        }

                        if (ImGui.Selectable("Deathroll", OpenWindow == OpenWindow.DeathRoll, ImGuiSelectableFlags.Disabled))
                        {
                            OpenWindow = OpenWindow.DeathRoll;
                        }
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
                
            if (ImGui.BeginChild($"GamblingRightSide", Vector2.Zero, false))
            {
                switch (OpenWindow)
                {
                    case OpenWindow.Overview:
                        DrawOverview();
                        break;
                    case OpenWindow.Blackjack:
                        DrawBlackJack();
                        break;
                    case OpenWindow.DeathRoll:
                        DrawDeathroll();
                        break;
                }
                  
            }
                
            ImGui.EndChild();
            ImGui.EndTable();
            ImGui.PopStyleVar();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public void DrawOverview()
    {
        ImGui.TextWrapped($"GambaGames Overview");
            
        ImGui.Spacing();
        ImGui.Spacing();
            
        ImGui.TextWrapped($"BlackJack");
        ImGui.Separator();
        ImGui.TextWrapped($"Players will try to get as close to 21 as possible without going over.");
        ImGui.TextWrapped("2 cards will be picked from the deck as their starting hand. Players can then chose to Hit, Split, Double Down, Stay, or Surrender.");
        ImGui.Bullet();
        ImGui.Text("Hit - Draw another card from the deck.");
        ImGui.Bullet();
        ImGui.TextWrapped("Split - If a player is dealt a pair (2 cards of the same value) they may split it into 2 separate hands and play 2 games at once. They will have to put down a second bet for the second hand and 2 more cards are drawn to complete each hand.");
        ImGui.Bullet();
        ImGui.TextWrapped("Double Down - The player can double their bet upon seeing their hand. Once a player doubles down 1 more card is drawn and they are not allowed to draw any more.");
        ImGui.Bullet();
        ImGui.TextWrapped("Stay - No more cards will be drawn for this player.");
        ImGui.Bullet();
        ImGui.TextWrapped("Surrender - The player ends their hand. The house will keep half of the player's bet.");
            
        ImGui.Spacing();
        ImGui.Spacing();
            
        ImGui.TextWrapped($"DeathRoll");
        ImGui.Separator();
        ImGui.TextWrapped($"1 Player will roll against the dealer and the first to reach 1 loses.");
        ImGui.TextWrapped("Both players will roll /dice 10. The higher roll will roll /dice first.");
        ImGui.TextWrapped("The other player will then roll the value that was rolled. Ex: /dice outputs 982, player 2 will roll /dice 982.");
        ImGui.TextWrapped("This will repeat until someone reaches 1.");
    }
        
    public void DrawBlackJack()
    {
        if (!GameInProgress)
        {
            try
            {
                ImGui.TextWrapped($"Below you can set the number of decks to use in the game! (Default: 2)");

                if (ImGui.SliderInt("", ref Decks, 1, 8))
                {
                    Deck.ClearDeck();
                    Deck.CreateDeck(Decks);
                    Deck.Shuffle();
                }

                ImGui.SameLine();

                if (ImGui.Button("Reshuffle", ImGuiHelpers.ScaledVector2(75, 25)))
                {
                    Deck.Shuffle();
                }

                ImGui.Separator();
                ImGui.TextWrapped(
                    $"You can also add players by targeting them, it is recommended to play in a party though.");
                ImGui.Separator();

                if (_clientState.LocalPlayer?.TargetObject != null &&
                    _clientState.LocalPlayer.TargetObject.ObjectKind == ObjectKind.Player)
                {
                    bool inparty = false;
                    foreach (var partyMember in _party)
                    {
                        if (_clientState.LocalPlayer.TargetObject.Name.TextValue == partyMember.Name.TextValue)
                            inparty = true;
                    }
                        
                    if (!inparty)
                    {
                        ImGui.TextWrapped("Add Targeted player: ");

                        ImGui.TextWrapped(_clientState.LocalPlayer.TargetObject.Name.TextValue);
                        ImGui.SameLine();
                        if (ImGui.Button("Add", ImGuiHelpers.ScaledVector2(40, 22)))
                        {
                            NonPartyplayers.Add(_clientState.LocalPlayer.TargetObject.Name.TextValue);
                        }

                        ImGui.SameLine();
                        if (ImGui.Button("Remove", ImGuiHelpers.ScaledVector2(65, 22)))
                        {
                            NonPartyplayers.Remove(_clientState.LocalPlayer.TargetObject.Name.TextValue);
                        }

                        ImGui.Separator();
                    }
                }

                ImGui.TextWrapped("Party Member Playing:");
                ImGui.Spacing();

                bool dealerPlaying = true;
                ImGui.Checkbox("Dealer (You)", ref dealerPlaying);
                    
                foreach (var partyMember in _party)
                {
                    if (partyMember.Name.TextValue == DealerName)
                    {
                        continue;
                    }

                    bool isPlaying;
                    PlayersBets.TryAdd(partyMember.Name.TextValue, 0.ToString());
                    if (!PartyPlaying.TryAdd(partyMember.Name.TextValue, false))
                    {
                        isPlaying = PartyPlaying[partyMember.Name.TextValue];
                    }
                    else
                    {
                        isPlaying = false;
                    }

                    if (ImGui.Checkbox(partyMember.Name.TextValue, ref isPlaying))
                    {
                        PartyPlaying[partyMember.Name.TextValue] = isPlaying;
                    };
                    ImGui.SameLine();
                    ImGui.PushItemWidth(150f.Scale());
                    ImGui.TextWrapped("\u2192 Bet: ");
                    ImGui.SameLine();
                    string bet = PlayersBets[partyMember.Name.TextValue];
                    ImGui.PushID($"{partyMember.Name.TextValue.Replace(" ", "_")}_bet_input");
                    if (ImGui.InputText(" ", ref bet, 10))
                    {
                        PlayersBets[partyMember.Name.TextValue] = string.Format(CultureInfo.InvariantCulture, "{0:n0}",
                            int.Parse(bet.Replace(",", "")));;
                    }
                }

                if (NonPartyplayers.Count > 0)
                {
                    ImGui.TextWrapped("Other Members Playing:");
                    foreach (var nonPartyplayer in NonPartyplayers)
                    {
                        PlayersBets.TryAdd(nonPartyplayer, 0.ToString());
                        if (ImGui.Button("X", ImGuiHelpers.ScaledVector2(20, 22)))
                        {
                            NonPartyplayers.Remove(nonPartyplayer);
                        }
                        ImGui.SameLine();
                        ImGui.TextWrapped(nonPartyplayer);
                        ImGui.SameLine();
                        ImGui.PushItemWidth(150f.Scale());
                        ImGui.TextWrapped("\u2192 Bet: ");
                        ImGui.SameLine();
                        string bet = PlayersBets[nonPartyplayer];
                        ImGui.PushID($"{nonPartyplayer.Replace(" ", "_")}_bet_input");
                        if (ImGui.InputText(" ", ref bet, 10))
                        {
                            PlayersBets[nonPartyplayer] =  string.Format(CultureInfo.InvariantCulture, "{0:n0}",
                                int.Parse(bet.Replace(",", "")));
                        }
                    }
                }

                ImGui.Spacing();

                if (ImGui.Button("Start Game", ImGuiHelpers.ScaledVector2(150, 50)))
                {
                    foreach (var player in PartyPlaying)
                    {
                        if (_party.All(x => x.Name.TextValue != player.Key)) PartyPlaying.Remove(player.Key);
                    }
                        
                    foreach (var player in PartyPlaying)
                    {
                        if(!player.Value) continue;
                        Players.Add(player.Key);
                    }

                    if (NonPartyplayers.Count > 0)
                    {
                        ImGui.TextWrapped("Other Member Playing:");
                        foreach (var nonPartyplayer in NonPartyplayers)
                        {
                            Players.Add(nonPartyplayer);
                        }
                    }

                    Players.Add(DealerName);

                    foreach (var player in Players)
                    {
                        Hands.Draw(player, Decks, true);
                        Hands.Draw(player, Decks, true);
                    }

                    GameInProgress = true;
                }
            }
            catch (Exception e)
            {
                    
            }
        }
        else
        {
            try
            {
                ImGui.TextWrapped("GAME IN PROGRESS");
                ImGui.SameLine(125f.Scale());
                ImGui.PushItemWidth(100f.Scale());
                ImGui.Combo("Chat Type", ref SelectedChatType, ChatTypes, ChatTypes.Length);
                ImGui.SameLine(ImGui.GetWindowWidth() - 80f.Scale());
                if (ImGui.Button("End Game", ImGuiHelpers.ScaledVector2(75f, 22)))
                {
                    ShowGame = true;
                    Hands.ClearHands();
                    HasSurrendered.Clear();
                    GameInProgress = false;
                    Players = new List<string>();
                    CanSurrender = new bool[16]{true,true,true,true,true,true,true,true,true,true,true,true,true,true,true,true};
                }

                if (GameIsOver && ShowGame)
                {
                    ImGui.SameLine(ImGui.GetWindowWidth() - 160f.Scale());
                    if (ImGui.Button("Results", ImGuiHelpers.ScaledVector2(75, 22)))
                    {
                        ShowGame = false;
                    }
                }

                ImGui.Separator();

                if (ShowGame)
                {
                    int counter = 0;
                    foreach (var player in Players)
                    {
                        if (player == DealerName)
                        {
                            ImGui.Separator();
                            ImGui.PushID($"{DealerName.Replace(" ", "_")}_Header");
                            if (ImGui.CollapsingHeader("Dealer (You)"))
                            {
                                if (Hands.HandValue(Hands.GetHand(player), player) < 21)
                                {
                                    if (!Hands.DealerStay(Hands.GetHand(DealerName)))
                                    {
                                        if (ImGui.Button("Hit", ImGuiHelpers.ScaledVector2(50, 22)))
                                        {
                                            Hands.Draw(player, Decks, false);
                                        }
                                        ImGui.Spacing();
                                        ImGui.PushID($"{DealerName.Replace(" ", "_")}_copy");
                                        if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                        {
                                            ImGui.SetClipboardText(PlayersHands[counter]);
                                            Notify.Success("Copied hand to clipboard");
                                        }
                                        ImGui.SameLine();
                                        ImGui.PushItemWidth(300f.Scale());
                                        ImGui.PushID($"{DealerName.Replace(" ", "_")}_hand");
                                        ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                        if (Hands.GetHand(DealerName).Split(" ").Length == 2)
                                        {
                                            if (HideCard)
                                            {
                                                string cards = Hands.GetHand(DealerName)
                                                    .Replace(Hands.GetHand(DealerName).Split(" ")[1], "??");
                                                PlayersHands[counter] =
                                                    $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {cards}";
                                            }
                                            else
                                            {
                                                PlayersHands[counter] =
                                                    $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})";
                                            }
                                            ImGui.SameLine();
                                            ImGui.Checkbox("Hide 2nd card", ref HideCard);

                                        }
                                        else
                                        {
                                            ImGui.SameLine();
                                            PlayersHands[counter] =
                                                $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})";
                                        }
                                    }
                                    else
                                    {
                                        if (Hands.GetHand(DealerName).Split(" ").Length == 2)
                                        {
                                            ImGui.PushItemWidth(300f.Scale());
                                            if (HideCard)
                                            {
                                                string cards = Hands.GetHand(DealerName)
                                                    .Replace(Hands.GetHand(DealerName).Split(" ")[1], "??");
                                                PlayersHands[counter] =
                                                    $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {cards}";
                                            }
                                            else
                                            {
                                                PlayersHands[counter] =
                                                    $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})";
                                            }

                                            ImGui.InputText($"", ref PlayersHands[counter], 200,
                                                ImGuiInputTextFlags.ReadOnly);
                                            ImGui.SameLine();
                                            ImGui.PushID($"{DealerName}_copy");
                                            if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                            {
                                                if (HideCard)
                                                {
                                                    string cards = Hands.GetHand(DealerName).Split(" ")[0] + " ??";
                                                    ImGui.SetClipboardText($"/{ChatTypes[SelectedChatType]} Dealer's Hand: {cards}");
                                                }
                                                else
                                                {
                                                    ImGui.SetClipboardText(
                                                        $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})");
                                                }
                                                Notify.Success("Copied hand to clipboard");
                                            }
                                            ImGui.SameLine();
                                            ImGui.Checkbox("Hide 2nd card", ref HideCard);
                                            ImGui.Text("Dealer Stay Rules Met");
                                        }
                                        else
                                        {
                                            PlayersHands[counter] =
                                                $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})";
                                            ImGui.PushItemWidth(300f.Scale());
                                            ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                            ImGui.SameLine();
                                            ImGui.PushID($"{DealerName}_copy");
                                            if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                            {
                                                ImGui.SetClipboardText(
                                                    $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})");
                                                Notify.Success("Copied hand to clipboard");
                                            }

                                            Hands.SetGameOver(DealerName);
                                            ImGui.Text("Dealer Stay Rules Met");
                                        }
                                    }
                                }
                                else
                                {
                                    ImGui.PushItemWidth(300f.Scale());
                                    ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                    ImGui.SameLine();
                                    if (Hands.HandValue(Hands.GetHand(player), player) > 21)
                                    {
                                        PlayersHands[counter] =
                                            $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)}) - BUST";
                                    }
                                    else
                                    {
                                        PlayersHands[counter] =
                                            $"/{ChatTypes[SelectedChatType]} Dealer's Hand: {Hands.GetHand(DealerName)} ({Hands.HandValue(Hands.GetHand(DealerName), player)})";
                                    }
                                    ImGui.SameLine();
                                    if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                    {
                                        ImGui.SetClipboardText(PlayersHands[counter]);
                                        Notify.Success("Copied hand to clipboard");
                                    }
                                }
                            }

                            counter++;
                            continue;
                        }

                        ImGui.PushID($"{player.Replace(" ", "_")}_header");
                        if (ImGui.CollapsingHeader(player.Replace(" 2", "")))
                        {
                            ImGui.Text("Bet: ");
                            ImGui.SameLine();
                            ImGui.PushItemWidth(105f.Scale());
                            ImGui.PushID($"{player.Replace(" ", "_")}_bet");
                            string bet = PlayersBets[player];
                            ImGui.InputText($"", ref bet, 200, ImGuiInputTextFlags.ReadOnly);
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                            {
                                ImGui.SetTooltip("Bet can not be modified mid-game");
                            }

                            if (!Hands.IsPlayerGameOver(player))
                            {
                                ImGui.SameLine();
                                ImGui.Spacing();
                                ImGui.SameLine();
                                ImGui.PushID($"{player.Replace(" ", "_")}_hit");
                                if (ImGui.Button("Hit", ImGuiHelpers.ScaledVector2(50, 22)))
                                {
                                    Hands.Draw(player, Decks, false);
                                    CanSurrender[counter] = false;
                                }

                                if (Hands.CanSplit(Hands.GetHand(player), player))
                                {
                                    ImGui.SameLine();
                                    ImGui.PushID($"{player.Replace(" ", "_")}_split");
                                    if (ImGui.Button("Split", ImGuiHelpers.ScaledVector2(50, 22)))
                                    {
                                        // Move the dealer hand and bet to the over 1 index
                                        // and insert the new "player" into the gap
                                        int indexToInsert = counter + 1;
                                        PlayersBets.Add(player + " 2", PlayersBets[player]);
                                        Array.Copy(PlayersHands, indexToInsert, PlayersHands, indexToInsert + 1,
                                            PlayersHands.Length - indexToInsert - 1);
                                        Players.Insert(indexToInsert, player + " 2");
                                        Hands.Split(player);
                                        CanSurrender[counter] = false;
                                    }
                                }

                                ImGui.SameLine();
                                ImGui.PushID($"{player.Replace(" ", "_")}_dd");
                                if (ImGui.Button("Double Down", ImGuiHelpers.ScaledVector2(100, 22)))
                                {
                                    Hands.DoubleDown(player, Decks);

                                    PlayersBets[player] =
                                        string.Format(CultureInfo.InvariantCulture, "{0:n0}",
                                            int.Parse(PlayersBets[player]
                                                .Replace(",", "")) * 2);
                                }

                                ImGui.SameLine();
                                ImGui.PushID($"{player.Replace(" ", "_")}_stay");
                                if (ImGui.Button("Stay", ImGuiHelpers.ScaledVector2(50, 22)))
                                {
                                    Hands.SetGameOver(player);
                                }

                                if (CanSurrender[counter])
                                {
                                    ImGui.SameLine();
                                    ImGui.PushID($"{player.Replace(" ", "_")}_surrender");
                                    if (ImGui.Button("Surrender", ImGuiHelpers.ScaledVector2(75, 22)))
                                    {
                                        HasSurrendered.Add(player);
                                        Hands.SetGameOver(player);
                                    }
                                }

                                ImGui.Spacing();
                                ImGui.PushID($"{player.Replace(" ", "_")}_copy");
                                if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                {
                                    ImGui.SetClipboardText(PlayersHands[counter]);
                                    Notify.Success("Copied hand to clipboard");
                                }

                                ImGui.SameLine();
                                ImGui.PushItemWidth(500f.Scale());
                                ImGui.PushID($"{player}_copy");
                                ImGui.PushID($"{player.Replace(" ", "_")}_hand");
                                if (player.Contains(" 2"))
                                {
                                    ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                    PlayersHands[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player.Replace(" 2", "")}'s Second Hand: {Hands.GetHand(player)} ({Hands.HandValue(Hands.GetHand(player), player)})";
                                }
                                else
                                {
                                    ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                    PlayersHands[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player}'s Hand: {Hands.GetHand(player)} ({Hands.HandValue(Hands.GetHand(player), player)})";
                                }

                                ImGui.PushID($"{player.Replace(" ", "_")}_copy");
                                if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                {
                                    ImGui.SetClipboardText(PlayersChoices[counter]);
                                    Notify.Success("Copied options to clipboard");
                                }

                                ImGui.SameLine();
                                ImGui.PushItemWidth(500f.Scale());
                                ImGui.PushID($"{player}_copy");
                                ImGui.PushID($"{player.Replace(" ", "_")}_hand");
                                ImGui.InputText($"", ref PlayersChoices[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                if (CanSurrender[counter] && Hands.CanSplit(Hands.GetHand(player), player))
                                {
                                    PlayersChoices[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player} Hit, Split, Double Down, Stay, or Surrender?";
                                }

                                if (CanSurrender[counter] && !Hands.CanSplit(Hands.GetHand(player), player))
                                {
                                    PlayersChoices[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player} Hit, Double Down, Stay, or Surrender?";
                                }

                                if (!CanSurrender[counter] && !Hands.CanSplit(Hands.GetHand(player), player))
                                {
                                    PlayersChoices[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player} Hit, Double Down, or Stay?";
                                }

                            }
                            else
                            {
                                ImGui.PushID($"{player.Replace(" ", "_")}_copy");
                                if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                                {
                                    ImGui.SetClipboardText(PlayersHands[counter]);
                                    Notify.Success("Copied hand to clipboard");
                                }

                                ImGui.SameLine();
                                ImGui.PushItemWidth(300f.Scale());
                                ImGui.PushID($"{player}_copy");
                                ImGui.PushID($"{player.Replace(" ", "_")}_hand");
                                ImGui.InputText($"", ref PlayersHands[counter], 200, ImGuiInputTextFlags.ReadOnly);
                                if (Hands.HandValue(Hands.GetHand(player), player) > 21)
                                {
                                    PlayersHands[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player}'s Hand: {Hands.GetHand(player)} ({Hands.HandValue(Hands.GetHand(player), player)}) - BUST";
                                }
                                else
                                {
                                    PlayersHands[counter] =
                                        $"/{ChatTypes[SelectedChatType]} {player}'s Hand: {Hands.GetHand(player)} ({Hands.HandValue(Hands.GetHand(player), player)})";
                                }
                            }
                        }

                        counter++;
                    }
                }

                if (Hands.IsGameOver(Players) && ShowGame)
                {
                    GameIsOver = true;
                    ImGui.Spacing();
                    ImGui.Text("Game over detected. Hit 'Results' at the top!");
                }

                if (!ShowGame)
                {
                    GameIsOver = true;

                    foreach (var player in Players)
                    {
                        bool hasSplit = false;
                        int playerRewards = 0;
                        if (DealerName == player) continue;
                        if (Players.Contains($"{player} 2") || player.EndsWith(" 2"))
                        {
                            if(!player.EndsWith(" 2")) SplitHandsResults.TryAdd(player, 0);
                            hasSplit = true;
                        };
                            
                        int dealerHandVal = Hands.HandValue(Hands.GetHand(DealerName), DealerName);

                        if (HasSurrendered.Contains(player))
                        {
                            if (hasSplit)
                            {
                                SplitHandsResults[player.Replace(" 2", "")] +=
                                    int.Parse(PlayersBets[player]
                                        .Replace(",", "")) / 2;
                                    
                                continue;
                            }
                                
                            playerRewards = int.Parse(PlayersBets[player]
                                .Replace(",", "")) / 2;
                                
                            GameResults +=
                                $"{player} \u2192 {string.Format(CultureInfo.InvariantCulture, "{0:n0}", playerRewards)}, ";

                            ImGui.Text(
                                $"{player} surrendered - Give {string.Format(CultureInfo.InvariantCulture, "{0:n0}", playerRewards)} gil");
                        }
                        else
                        {
                            if (dealerHandVal > 21)
                            {
                                if (Hands.HandValue(Hands.GetHand(player), player) <= 21)
                                {
                                    if (Hands.HandValue(Hands.GetHand(player), player) == 21)
                                    {
                                        if (hasSplit)
                                        {
                                            SplitHandsResults[player.Replace(" 2", "")] +=
                                                (int)(int.Parse(PlayersBets[player]
                                                    .Replace(",", "")) * (2));
                                            continue;
                                        }

                                        if (Hands.IsHandNatural(player))
                                        {
                                            playerRewards =
                                                (int)(int.Parse(PlayersBets[player]
                                                    .Replace(",", "")) * (2.5));
                                        }
                                        else
                                        {
                                            playerRewards =
                                                (int)(int.Parse(PlayersBets[player]
                                                    .Replace(",", "")) * (2));
                                        }
                                    }
                                    else
                                    {
                                        if (hasSplit)
                                        {
                                            SplitHandsResults[player.Replace(" 2", "")] +=
                                                (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                    
                                            continue;
                                        }
                                            
                                        playerRewards = (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                    }
                                }
                                else
                                {
                                    if (hasSplit) continue;
                                        
                                    playerRewards = 0;
                                }
                            }
                            else
                            {
                                if (Hands.HandValue(Hands.GetHand(player), player) > 21)
                                {
                                    if (hasSplit) continue;
                                        
                                    playerRewards = 0;
                                } 
                                else if (Hands.HandValue(Hands.GetHand(player), player) < dealerHandVal)
                                {
                                    if (hasSplit) continue;

                                    playerRewards = 0;
                                } 
                                else if (Hands.HandValue(Hands.GetHand(player), player) == dealerHandVal)
                                {
                                    if (dealerHandVal == 21 && Hands.IsHandNatural(player) && !Hands.IsHandNatural(DealerName))
                                    {
                                        if (hasSplit)
                                        {
                                            SplitHandsResults[player.Replace(" 2", "")] +=
                                                (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                            continue;
                                        }
                                            
                                        playerRewards = (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                    } 
                                    else if (hasSplit)
                                    {
                                        SplitHandsResults[player.Replace(" 2", "")] +=
                                            int.Parse(PlayersBets[player].Replace(",", ""));
                                            
                                        continue;
                                    }
                                    else
                                    {
                                        playerRewards = int.Parse(PlayersBets[player].Replace(",", ""));
                                    }
                                } 
                                else if (Hands.HandValue(Hands.GetHand(player), player) == 21)
                                {
                                    if (hasSplit)
                                    {
                                        SplitHandsResults[player.Replace(" 2", "")] +=
                                            (int)(int.Parse(PlayersBets[player]
                                                .Replace(",", "")) * (2));
                                        continue;
                                    }

                                    if (Hands.IsHandNatural(player))
                                    {
                                        playerRewards =
                                            (int)(int.Parse(PlayersBets[player]
                                                .Replace(",", "")) * (2.5));
                                    }
                                    else
                                    {
                                        playerRewards =
                                            (int)(int.Parse(PlayersBets[player]
                                                .Replace(",", "")) * (2));
                                    }
                                }
                                else if (Hands.HandValue(Hands.GetHand(player), player) > dealerHandVal)
                                {
                                    if (hasSplit)
                                    {
                                        SplitHandsResults[player.Replace(" 2", "")] +=
                                            (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                    
                                        continue;
                                    }
                                        
                                    playerRewards = (int)(int.Parse(PlayersBets[player].Replace(",", "")) * (2));
                                } 
                            }

                            if (playerRewards == 0)
                            {
                                GameResults += $"{player} \u2192 LOSS, "; // Is this loss?
                                    
                                ImGui.Text(
                                    $"{player} lost");
                            }
                            else
                            {
                                GameResults +=
                                    $"{player} \u2192 {string.Format(CultureInfo.InvariantCulture, "{0:n0}", playerRewards)}, ";

                                ImGui.Text(
                                    $"{player} won - Give {string.Format(CultureInfo.InvariantCulture, "{0:n0}", playerRewards)} gil");
                            }
                        }
                    }
                        
                    // Process all of the split hands
                    foreach (var player in SplitHandsResults)
                    {
                        if (player.Value == 0)
                        {
                            GameResults += $"{player.Key} \u2192 LOSS, "; // Is this loss?
                                    
                            ImGui.Text(
                                $"{player.Key} lost");
                        }
                        else
                        {
                            GameResults +=
                                $"{player.Key} \u2192 {string.Format(CultureInfo.InvariantCulture, "{0:n0}", player.Value)}, ";

                            ImGui.Text(
                                $"{player.Key} won - Give {string.Format(CultureInfo.InvariantCulture, "{0:n0}", player.Value)} gil");
                        }
                    }

                    SplitHandsResults = new Dictionary<string, int>();
                    
                    ImGui.Separator();
                    string resultString = $"/{ChatTypes[SelectedChatType]} {GameResults} Good Game!";
                    ImGui.PushID($"final_copy");
                    if (ImGui.Button("Copy", ImGuiHelpers.ScaledVector2(50, 22)))
                    {
                        ImGui.SetClipboardText(resultString);
                        Notify.Success("Copied hand to clipboard");
                    }
                    ImGui.SameLine();
                    ImGui.PushItemWidth(300f.Scale());
                    ImGui.PushID($"Final_result_string");
                    ImGui.InputText($"", ref resultString, 200, ImGuiInputTextFlags.ReadOnly);
                    GameResults = "";
                }
            }
            catch (Exception e)
            {
                ImGui.TextWrapped(e.ToString());
            }
        }
    }
        
    public static void DrawDeathroll()
    {
        ImGui.TextWrapped($"Not implemented yet!");
            
        ImGui.Separator();
    }
}
    
public enum OpenWindow
{
    Overview = 1,
    Blackjack = 2,
    DeathRoll = 3
}