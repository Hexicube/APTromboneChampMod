using System.Collections.Generic;
using System.Linq;

namespace APTromboneChampMod;

public struct Track {
    public long ID;
    public string Name;
    public int Difficulty;
    public bool Unsafe;
    public string DLC;

    public Track(long ID, string Name, int Difficulty, bool Unsafe = false, string DLC = "Base") {
        this.ID = ID;
        this.Name = Name;
        this.Unsafe = Unsafe;
        this.DLC = DLC;
    }

    public override bool Equals(object obj)
    {
        if (obj is null) return false;
        if (obj is Track track) return base.Equals(track);
        if (obj is string name) return name == Name;
        return false;
    }
}

public class APTracks {
    public static Track[] TRACKS = [
        new Track(  1, "Are U Ready", 7),
        new Track(  2, "Arirang", 3),
        new Track(  3, "Auld Lang Syne", 7),
        new Track(  4, "Baboons!", 6),
        new Track(  5, "Bald Mountain", 7),
        new Track(  6, "Ball Game", 3),
        new Track(  7, "Barber of Seville", 7),
        new Track(  8, "Beethoven's Fifth", 7),
        new Track(  9, "Blue Danube", 5),
        new Track( 10, "Bumblebee", 9),
        new Track( 11, "Carol of the Bells", 7),
        new Track( 12, "Chop Waltz", 6),
        new Track( 13, "Commander Tokyo", 9),
        new Track( 14, "Danny Boy", 3),
        new Track( 15, "Danse Macabre", 8),
        new Track( 16, "Eine (CHAMP MIX)", 10),
        new Track( 17, "Eine Kleine", 5),
        new Track( 18, "Entertainer", 7),
        new Track( 19, "Four Seasons (Summer)", 9),
        new Track( 20, "Funiculi Funicula", 6),
        new Track( 21, "Gladiators", 8),
        new Track( 22, "God Save the King", 2),
        new Track( 23, "Gymnopédie", 5),
        new Track( 24, "Habanera", 5),
        new Track( 25, "Happy Birthday", 5),
        new Track( 26, "Hava Nagila", 5),
        new Track( 27, "Hello! Ma Baby", 6),
        new Track( 28, "Hino do Brasil", 7),
        new Track( 29, "Hungarian Dance", 7, true),
        new Track( 30, "Hungarian Rhapsody", 9),
        new Track( 31, "Jarabe Tapatío", 9),
        new Track( 32, "Jasmine Flower", 3),
        new Track( 33, "Jingle Bells", 5),
        new Track( 34, "Korobeiniki", 7),
        new Track( 35, "Long-Tail Limbo", 5),
        new Track( 36, "Mars", 4),
        new Track( 37, "Marseillaise", 7),
        new Track( 38, "Martian Killbots", 3),
        new Track( 39, "Merry Gentlemen", 7),
        new Track( 40, "Mountain King", 8),
        new Track( 41, "O Canada", 3),
        new Track( 42, "O Christmas Tree", 4),
        new Track( 43, "Ode to Joy", 2),
        new Track( 44, "Oh Chanukah!", 8),
        new Track( 45, "Old Gray Mare", 5),
        new Track( 46, "Old MacDonald", 7),
        new Track( 47, "Rhapsody in Blue", 8),
        new Track( 48, "Rising Sun Blues", 4),
        new Track( 49, "Rosamunde", 6),
        new Track( 50, "Round the Mountain", 7),
        new Track( 51, "Sailor's Hornpipe", 10),
        new Track( 52, "Sakura", 2),
        new Track( 53, "Silent Night", 4),
        new Track( 54, "SkaBIRD", 6),
        new Track( 55, "Skeleton Rag", 6),
        new Track( 56, "Skip To My Lou", 5),
        new Track( 57, "St James Trombonery", 4),
        new Track( 58, "Stars & Stripes", 8, true),
        new Track( 59, "Star-Spangled", 5),
        new Track( 60, "Sugar Plum Fairy", 5),
        new Track( 61, "T. Champ Medley", 7),
        new Track( 62, "Taps", 3),
        new Track( 63, "The Can-Can", 8),
        new Track( 64, "The Ritz", 7),
        new Track( 65, "The Riverside", 5),
        new Track( 66, "The Saints", 5),
        new Track( 67, "Toccata & Fugue", 8),
        new Track( 68, "Trombone Fuerte", 9),
        new Track( 69, "Trombone Skyze", 4),
        new Track( 70, "Trombone Skyze (Nasty)", 8),
        new Track( 71, "W. Post March", 7, true),
        new Track( 72, "Warm-Up", 1),
        new Track( 73, "William Tell", 8),
        new Track( 74, "Zarathustra", 1),
        // celeste
        new Track(201, "Confronting Myself", 7, true, "Celeste"),
        new Track(202, "First Steps", 7, true, "Celeste"),
        new Track(203, "Heart of the Mountain", 7, true, "Celeste"),
        new Track(204, "Madeline and Theo", 6, true, "Celeste"),
        new Track(205, "Reach for the Summit", 6, true, "Celeste"),
        new Track(206, "Reflection", 6, true, "Celeste"),
        new Track(207, "Resurrections", 6, true, "Celeste"),
        new Track(208, "Scattered and Lost", 8, true, "Celeste"),
        new Track(209, "Spirit of Hospitality", 6, true, "Celeste"),
        new Track(210, "Starjump", 8, true, "Celeste"),
        // pizza tower
        new Track(251, "Bye Bye There!", 7, true, "Pizza Tower"),
        new Track(252, "Cold Spaghetti", 8, true, "Pizza Tower"),
        new Track(253, "Death I Deservioli", 8, true, "Pizza Tower"),
        new Track(254, "ET Wahwahs", 8, true, "Pizza Tower"),
        new Track(255, "Funiculi Holiday", 7, true, "Pizza Tower"),
        new Track(256, "Good Eatin'", 8, true, "Pizza Tower"),
        new Track(257, "It's Pizza Time!", 8, true, "Pizza Tower"),
        new Track(258, "Kid's Menu", 9, true, "Pizza Tower"),
        new Track(259, "Oregano Mirage", 9, true, "Pizza Tower"),
        new Track(260, "Pizza Deluxe", 7, true, "Pizza Tower"),
        new Track(261, "Pumpin' Hot Stuff", 7, true, "Pizza Tower"),
        new Track(262, "Put On A Show!", 7, true, "Pizza Tower"),
        new Track(263, "Unexpectancy", 9, true, "Pizza Tower"),
        new Track(264, "Yeehaw", 8, true, "Pizza Tower"),
        // undertale / deltarune
        new Track(301, "ASGORE", 8, true, "UTDR"),
        new Track(302, "BIG SHOT", 7, true, "UTDR"),
        new Track(303, "Black Knife", 7, true, "UTDR"),
        new Track(304, "Bonetrousle", 8, true, "UTDR"),
        new Track(305, "CYBER'S WORLD?", 7, true, "UTDR"),
        new Track(306, "Dark Sanctuary", 7, true, "UTDR"),
        new Track(307, "Dummy!", 9, true, "UTDR"),
        new Track(308, "GUARDIAN", 8, true, "UTDR"),
        new Track(309, "Hopes and Dreams", 8, true, "UTDR"),
        new Track(310, "It's TV Time!", 8, true, "UTDR"),
        new Track(311, "Killer Queen", 8, true, "UTDR"),
        new Track(312, "Megalovania", 9, true, "UTDR"),
        new Track(313, "Metal Crusher", 9, true, "UTDR"),
        new Track(314, "Pandora Palace", 7, true, "UTDR"),
        new Track(315, "Rude Buster", 9, true, "UTDR"),
        new Track(316, "Scarlet Forest", 6, true, "UTDR"),
        new Track(317, "SWORD", 7, true, "UTDR"),
        new Track(318, "Third Sanctuary", 9, true, "UTDR"),
        new Track(319, "True Hero", 8, true, "UTDR"),
        new Track(320, "TV World", 7, true, "UTDR"),
        new Track(321, "World Revolving", 8, true, "UTDR"),
    ];

    public static IEnumerable<Track> GetTrackList(APSettings settings) {
        foreach (Track track in TRACKS) {
            if (
                track.Difficulty >= settings.MinDiff &&
                track.Difficulty <= settings.MaxDiff &&
                (settings.Unsafe || !track.Unsafe) &&
                (
                    track.DLC == "Base" ||
                    (track.DLC == "Celeste" && settings.Celeste) ||
                    (track.DLC == "Pizza Tower" && settings.PizzaTower) ||
                    (track.DLC == "UTDR" && settings.UndertaleDeltarune)
                )
            ) {
                yield return track;
            }
        }
    }

    public static void SetGoalTrack(APSettings settings, string name) {
        if (settings.GoalTracks == 0) settings.GoalTrack = null;
        else settings.GoalTrack = GetTrackList(settings).First(track => track.Name == name);
    }
}