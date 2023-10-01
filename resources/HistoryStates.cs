using System.Text.RegularExpressions;

namespace history {
    public class State {
        public string file;
        public int ownerLine;
        public string owner;
        public Dictionary<string,int> cores = new Dictionary<string, int>();
    }
    class HistoryStates {
        static void Main(string[] args) {
            string[] stateFiles = Directory.GetFiles("../history/states","*.txt");
            string observer = "SWE";
            string wastelandTag = "WST";
            // The shit that isn't going to be wasteland
            string[] tags = new String[43] {observer,"ALB","AST","AUS","BEL","BRA","BUL","CAN","CHI","CZE","DEN","ENG","EST","FIN","FRA","GER","GRE","HOL","HUN","IRE","IRQ","ITA","JAP","LAT","LIT","LUX","MAN","MEX","NOR","NZL","POL","RAJ","ROM","SAF","SIA","SOV","SPR","USA","VEN","VIC","YUG","ETH","PER"};
            // TagBeingReplaced TagToReplaceWith
            Dictionary<string,string> replaceTags = new Dictionary<string, string>(){
                {"INS","HOL"},
                {"PHI","USA"},
                {"GXC","CHI"},
                {"YUN","CHI"},
                {"SHX","CHI"},
                {"PRC","CHI"},
                {"SIK","CHI"},
                {"XSM","CHI"},
                {"AFA","ETH"},
                {"MEN","MAN"},
                {"MAL","ENG"}
            };
            // Shit to not core to new owner but core to observer
            string[] dontCore = {"MAL","PHI","INS"};
            List<State> states = new List<State>();
            // Tags to make wasteland in no asia, put original owner tag in vanilla, might have to do shit like indochina manually boohoo
            string[] noAsia = {"USA","AST","NZL","MEX","RAJ","MAN","SIA","JAP","INS","MAL","PHI","CHI","MEN","YUN","GXC","SIK","SHX","XSM","PRC"};

            /////////////////////////////////////////////////////////

            for (int i = 0; i < stateFiles.Length; i++) {
                states.Add(getState(stateFiles[i]));
                processState(states[i], observer, wastelandTag, tags, replaceTags, dontCore, noAsia);
            }
            Console.WriteLine("DONE");

            Console.ReadKey();
        }

        public static State getState(string file) {
            State state = new State();
            state.file = file;
            string[] fileLines = File.ReadAllLines(file);
            Dictionary<string,int> owners = new Dictionary<string, int>();
            for (int i = 0; i < fileLines.Length; i++) {
                if (Regex.IsMatch(fileLines[i],@".*owner =.*")) {
                    owners.Add(fileLines[i].Substring(fileLines[i].Length-3),i);
                }
                if (Regex.IsMatch(fileLines[i],@".*add_core_of =.*") && !state.cores.ContainsKey(fileLines[i].Substring(fileLines[i].Length-3))) {
                    
                    state.cores.Add(fileLines[i].Substring(fileLines[i].Length-3),i);
                }
            }

            if(owners.Count !=0 ) { // You want to add the first owner, incase there is some history that adds a second owner
                state.owner = owners.First().Key;
                state.ownerLine = owners.First().Value;
            }

            return state;
        }

        public static void processState(State state, string observer, string wastelandTag, string[] tags, Dictionary<string,string> replaceTags, string[] dontCore, string[] noAsia) {
            string[] fileLines = File.ReadAllLines(state.file);
            Console.WriteLine(state.owner);
            for (int i = 0; i < fileLines.Length; i++) {
                if(i == state.ownerLine) {
                    if(noAsia.Contains(state.owner)) {
                        for (int x = 0; x < fileLines.Length; x++) {
                            if(Regex.IsMatch(fileLines[x],@".*history.*")) {
                                if(Regex.IsMatch(fileLines[x],@".*{.*")) {
                                    fileLines[x] = "history={ 1936.1.1.13 = { owner = WST add_core_of = WST }";
                                } else {
                                    fileLines[x+1] = "{ 1936.1.1.13 = { owner = WST add_core_of = WST }";
                                }
                            }
                        }
                    }

                    // Replace tags, if owner is in replaceTags then replace owner with correct one
                    if(replaceTags.ContainsKey(state.owner)) {
                        fileLines[i] = fileLines[i].Replace(state.owner,replaceTags[state.owner]);
                        if(dontCore.Contains(state.owner)) {
                            fileLines[state.cores[state.owner]] = fileLines[state.cores[state.owner]].Replace(state.owner,observer);
                        } else {
                            fileLines[state.cores[state.owner]] = fileLines[state.cores[state.owner]].Replace(state.owner,replaceTags[state.owner]);
                        }
                    }

                    // Wasteland, if owner isn't in tags replace with wastelandTag
                    if(!tags.Contains(state.owner) && !replaceTags.ContainsKey(state.owner) && state.owner != "RAJ") { // RAJ Doesn't have cores on Indian land in history files nor is there any effect that adds them but they still magically get them
                        fileLines[i] = "        owner = " + wastelandTag;
                         for (int x = 0; x < fileLines.Length; x++) {
                            if(Regex.IsMatch(fileLines[x],@".*manpower =.*")) { // Set manpower to 1
                                fileLines[x] = "    manpower = 1";
                            }
                            if(Regex.IsMatch(fileLines[x],@".*victory_points =.*")) { // Remove victory points
                                int y = x;
                                while (!Regex.IsMatch(fileLines[y],@".*}.*"))
                                {
                                    fileLines[y] = "";
                                    y++;
                                }
                                fileLines[y] = ""; // Or closing bracket wont get removed
                            }
                        }
                    } else {
                        if(state.cores.ContainsKey(state.owner)) { // Colony cores, if owner is in tags and it's owner has a core then make them all owner cores
                            
                            foreach (var kvp in state.cores) {
                                fileLines[kvp.Value] = fileLines[kvp.Value].Replace(kvp.Key,state.owner);
                            }
                        } else { // Unnecesary cores, if owner is in tags and it's owner doesn't have a core then make then all observer cores
                            foreach (var kvp in state.cores) {
                                if(!tags.Contains(kvp.Key)) { // Keep colony cores for tags, eg Nefoundland and Labrador should stay Canadian core
                                    fileLines[kvp.Value] = fileLines[kvp.Value].Replace(kvp.Key,observer);
                                }
                            }
                        }
                    }

                    
                }
            }
            File.WriteAllText(state.file, string.Join("\r\n", fileLines) + "\r\n");
        }
      
    }
}