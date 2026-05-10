using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BaboonAPI.Hooks.Tracks;
using BaboonAPI.Hooks.Tracks.Collections;
using BaboonAPI.Utility;
using Microsoft.FSharp.Core;
using UnityEngine;

namespace APTromboneChampMod;

public class TrackCollection(Plugin _plugin) : BaseTromboneCollection("AP", "Archipelago", "All tracks required for Archipelago")
{
    private static string[] TRACKS = [
        "Are U Ready", "Arirang", "Auld Lang Syne", "Baboons!", "Bald Mountain", "Ball Game", "Barber of Seville",
        "Beethoven's Fifth", "Blue Danube", "Bumblebee", "Carol of the Bells", "Chop Waltz", "Commander Tokyo",
        "Danny Boy", "Danse Macabre", "Eine (CHAMP MIX)", "Eine Kleine", "Entertainer", "Four Seasons (Summer)",
        "Funiculi Funicula", "Gladiators", "God Save the King", "Gymnopédie", "Habanera", "Happy Birthday",
        "Hava Nagila", "Hello! Ma Baby", "Hino do Brasil", "Hungarian Dance", "Hungarian Rhapsody",
        "Jarabe Tapatío", "Jasmine Flower", "Jingle Bells", "Korobeiniki", "Long-Tail Limbo", "Mars", "Marseillaise",
        "Martian Killbots", "Merry Gentlemen", "Mountain King", "O Canada", "O Christmas Tree", "Ode to Joy",
        "Oh Chanukah!", "Old Gray Mare", "Old MacDonald", "Rhapsody in Blue", "Rising Sun Blues", "Rosamunde",
        "Round the Mountain", "Sailor's Hornpipe", "Sakura", "Silent Night", "SkaBIRD", "Skeleton Rag", "Skip To My Lou",
        "St James Trombonery", "Stars & Stripes", "Star-Spangled", "Sugar Plum Fairy", "T. Champ Medley", "Taps",
        "The Can-Can", "The Ritz", "The Riverside", "The Saints", "Toccata & Fugue", "Trombone Fuerte", "Trombone Skyze",
        "Trombone Skyze (Nasty)", "W. Post March", "Warm-Up", "William Tell", "Zarathustra",
        
        "Confronting Myself", "First Steps", "Heart of the Mountain", "Madeline and Theo", "Reach for the Summit",
        "Reflection", "Resurrections", "Scattered and Lost", "Spirit of Hospitality", "Starjump",
        
        "Bye Bye There!", "Cold Spaghetti", "Death I Deservioli", "ET Wahwahs", "Funiculi Holiday", "Good Eatin'",
        "It's Pizza Time!", "Kid's Menu", "Oregano Mirage", "Pizza Deluxe", "Pumpin' Hot Stuff", "Put On A Show!",
        "Unexpectancy", "Yeehaw",
        
        "ASGORE", "BIG SHOT", "Black Knife", "Bonetrousle", "CYBER'S WORLD?", "Dark Sanctuary", "Dummy!", "GUARDIAN",
        "Hopes and Dreams", "It's TV Time!", "Killer Queen", "Megalovania", "Metal Crusher", "Pandora Palace",
        "Rude Buster", "Scarlet Forest", "SWORD", "Third Sanctuary", "True Hero", "TV World", "World Revolving"
    ];
    
    public override IEnumerable<TromboneTrack> BuildTrackList()
    {
        ArrayList unseen = new ArrayList(TRACKS);
        foreach (TromboneTrack track in TrackLookup.allTracks()) {
            if (unseen.Contains(track.trackname_short)) {
                unseen.Remove(track.trackname_short);
                yield return track;
            }
            // else Plugin.Logger.LogInfo($"Unknown track: {track.trackname_short}"); // custom tracks
        }
        foreach (string missed in unseen) Plugin.Logger.LogInfo($"Missed track: {missed}"); // TODO: notify player
    }

    public override Coroutines.YieldTask<FSharpResult<Sprite, string>> LoadSprite()
    {
        var path = Path.Combine(Path.GetDirectoryName(_plugin.Info.Location), "Assets", "collection.png");
        return BaboonAPI.Utility.Unity.loadTexture(path).Select(result =>
            ResultModule.Map(
                FuncConvert.FromFunc((Texture2D tex) =>
                    Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero)), result));
    }
}

public class TrackCollectionListener(Plugin _plugin) : TrackCollectionRegistrationEvent.Listener {
    public IEnumerable<TromboneCollection> OnRegisterCollections() {
        yield return new TrackCollection(_plugin);
    }
}