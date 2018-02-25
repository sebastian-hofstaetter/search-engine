using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace stopwords
{

    //[DisassemblyDiagnoser(printAsm: true, printSource: true, recursiveDepth: 3)]
    //[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions)]

    public class StopWordChecker
    {
        private static string[] _stopWords =
        {
            "a","an","and","also","all","are","as","at","be","been",
            "by","but","for","from","have","has","had","he","in","is",
            "it","its","more","my","new","not","of","on","or",
            "page","part","that","the","this","to","s","she","was","were",
            "will","with","i","you","they","up","so","if","would","make"
        };

        // 2x2500 random words from wikipedia
        private static string[] _testWords =
        {
            "federico","pizarro","born","september","is","an","argentine","handball","player","for","unlu","and","the","argentine","national","team","he","competed","for","the","argentine","national","team","at","the","summer","olympics","in","london","he","participated","at","the","world","men","s","handball","championship","in","qatar","the","cm","nebelwerfer","cm","nbw","was","a","german","multiple","rocket","launcher","used","in","the","second","world","war","it","served","with","units","of","the","so","called","nebeltruppen","the","german","equivalent","of","the","u","the","cm","nbw","was","a","six","barrelled","rocket","launcher","mounted","on","a","two","wheeled","carriage","two","stabilizer","arms","and","a","spade","under","the","towing","ring","served","to","steady","the","carriage","while","firing","it","used","two","different","rockets","the","open","metal","frames","of","the","launcher","were","sized","to","fit","the","centimetres","in","rocket","but","adapter","rails","were","provided","to","allow","the","centimetres","in","rockets","to","fit","the","cm","wurfk","rper","spreng","explosive","missile","rocket","weighed","kilograms","lb","and","had","a","kilograms","lb","high","explosive","warhead","many","launchers","were","converted","from","to","use","the","new","cm","wurfk","rper","rockets","as","the","cm","nebelwerfer","the","and","cm","rockets","could","also","be","fired","from","their","packing","cases","packkiste","which","had","short","hinged","legs","to","adjust","elevation","they","could","also","be","mounted","in","groups","of","four","on","wooden","schwere","wurfger","t","heavy","missile","device","or","tubular","metal","schwere","wurfger","t","swg","launch","frames","the","external","dimensions","of","the","packing","cases","were","identical","so","no","adapter","was","needed","for","the","smaller","rockets","a","sequence","showing","swg","launchers","being","used","against","warsaw","during","the","warsaw","uprising","in","positioning","a","rocket","in","a","swg","firing","frame","the","rockets","have","just","been","fired","note","the","piles","of","used","packing","crates","not","all","the","rockets","have","been","fired","at","the","same","time","four","rockets","in","flight","the","cm","nbw","was","organized","into","batteries","of","six","launchers","with","three","batteries","per","battalion","these","battalions","were","concentrated","in","independent","werfer","regimenter","and","brigaden","the","bergen","corpus","of","london","teenage","language","colt","is","a","data","set","of","samples","of","spoken","english","that","was","compiled","in","from","tape","recorded","and","transcribed","conversations","by","teens","between","the","ages","of","and","in","london","schools","this","corpus","which","has","been","tagged","for","part","of","speech","using","the","claws","tagset","is","one","of","the","linguistic","research","projects","housed","at","the","university","of","bergen","in","norway","linguistic","analysis","based","on","this","corpus","material","has","appeared","in","the","book","trends","in","teenage","talk","and","later","journal","articles","volts","is","an","album","by","ac","dc","released","as","a","part","named","disc","four","on","the","bonfire","box","set","released","in","november","the","album","is","a","compilation","of","some","alternative","versions","of","songs","recorded","for","the","albums","let","there","be","rock","and","highway","to","hell","and","some","songs","previously","released","a","hidden","track","containing","various","interviews","appears","after","a","short","amount","of","silence","following","the","last","track","last","of","the","conquerors","is","the","debut","novel","by","african","american","journalist","and","editor","william","gardner","smith","the","novel","concerns","the","author","s","experience","as","an","african","american","gi","s","serving","in","the","racially","segregated","united","states","army","in","us","occupied","germany","after","world","war","ii","the","protagonist","hayes","dawkins","has","an","affair","with","ilse","a","white","german","woman","together","he","and","ilse","struggle","against","racist","army","officers","and","policies","to","sustain","a","relationship","that","some","white","soldiers","condemn","although","there","are","also","many","friendly","whites","who","help","them","last","of","the","conquerors","depicts","post","nazi","germany","as","more","racially","tolerant","than","the","united","states","wang","xueqin","born","january","in","jiangsu","is","a","chinese","long","distance","runner","she","competed","in","the","marathon","at","the","summer","olympics","placing","nd","with","a","time","of","nils","j","diaz","is","a","former","chairman","of","the","nuclear","regulatory","commission","dr","diaz","s","term","as","chairman","ended","on","june","he","was","a","nuclear","engineering","professor","and","chairman","at","the","university","of","florida","in","gainesville","florida","from","the","s","to","the","s","he","is","also","a","uf","alumnus","christopher","alder","was","a","trainee","computer","programmer","and","former","british","army","paratrooper","who","had","served","in","the","falklands","war","and","was","commended","for","his","service","with","the","army","in","northern","ireland","he","died","while","in","police","custody","at","queen","s","gardens","police","station","kingston","upon","hull","in","april","the","case","became","a","cause","c","l","bre","for","civil","rights","campaigners","in","the","united","kingdom","he","had","earlier","been","the","victim","of","an","assault","outside","a","nightclub","and","was","taken","to","hull","royal","infirmary","where","possibly","as","a","result","of","his","head","injury","staff","said","his","behaviour","was","extremely","troublesome","on","arrival","at","the","police","station","alder","was","partially","dragged","and","partially","carried","handcuffed","and","unconscious","from","a","police","van","and","placed","on","the","floor","of","the","custody","suite","officers","chatted","between","themselves","and","speculated","that","he","was","faking","illness","twelve","minutes","later","one","of","the","officers","present","noticed","that","alder","was","not","making","any","breathing","noises","and","although","resuscitation","was","attempted","he","was","pronounced","dead","at","the","scene","a","post","mortem","indicated","that","the","head","injury","alone","would","not","have","killed","him","a","coroner","s","jury","in","returned","a","verdict","that","alder","was","unlawfully","killed","in","five","police","officers","went","on","trial","charged","with","alder","s","manslaughter","and","misconduct","in","public","office","but","were","acquitted","on","the","orders","of","the","judge","in","an","independent","police","complaints","commission","report","concluded","that","four","of","the","officers","present","in","the","custody","suite","when","alder","died","were","guilty","of","the","most","serious","neglect","of","duty","in","november","the","government","formally","apologised","to","alder","s","family","in","the","european","court","of","human","rights","admitting","that","it","had","breached","its","obligations","with","regard","to","preserving","life","and","ensuring","no","one","is","subjected","to",
            "inhuman","or","degrading","treatment","christopher","ibikunle","alder","june","april","was","a","black","british","man","of","nigerian","descent","born","in","hull","in","he","joined","the","british","army","at","the","age","of","and","served","in","the","parachute","regiment","for","six","years","after","leaving","the","army","he","first","settled","in","andover","hampshire","before","relocating","to","dagger","lane","hull","in","in","he","was","taking","a","college","course","in","computer","skills","in","hull","he","had","two","sons","who","had","remained","with","their","mother","in","the","andover","area","when","their","parents","separated","at","around","pm","on","march","alder","went","out","for","the","evening","in","hull","with","two","friends","visiting","several","local","bars","and","a","fast","food","restaurant","before","alder","suggested","going","on","to",
            "the","waterfront","club","later","renamed","the","sugar","mill","a","nightclub","on","prince","s","dock","street","in","the","old","town","area","of","the","city","his","companions","who","later","testified","that","at","this","stage","of","the","evening","alder","had","drunk","only","two","pints","of","lager","and","two","bottles","of","beck","s","beer","and","seemed","sober","declined","the","invitation","two","police","officers","pc","nigel","dawson","and","pc","neil","blakey","who","had","arrived","shortly","after","the","ambulance","in","a","marked","patrol","car","made","no","attempt","to","speak","to","alder","they","consulted","with","the","club","s","manager","who","took","them","inside","to","review","the","club","s","cctv","footage","of","the","incident","a","message","they","sent","to","their","control","room","at","this","time","indicates","that","they","had","already","assumed","alder","was","very","drunk","despite","not","having","spoken","to","him","or","having","been","told","this","by","any","of","the","witnesses","they","spoke","with","the","ambulance","arrived","at","the","hospital","at","am","where","alder","was","described","by","one","witness","who","dealt","with","him","as","confused","and","dazed","and","generally","abusive","one","of","the","paramedics","from","the","ambulance","crew","who","had","transported","him","there","stated","that","alder","asked","where","am","i","what","s","happened","one","of","the","nurses","who","treated","him","also","stated","that","in","addition","to","being","abusive","and","swearing","at","her","he","was","asking","where","am",
            "i","what","am","i","doing","two","police","officers","who","were","present","in","the","emergency","department","on","an","unrelated","matter","intervened","at","one","point","and","asked","him","to","cooperate","with","the","nursing","staff","gornja","suvaja","cyrillic","is","a","village","in","the","municipality","of","bosanska","krupa","bosnia","and","herzegovina","coordinates","n","e","n","e","henriette","koulla","born","september","is","a","cameroonian","female","volleyball","player","she","is","a","member","of","the","cameroon","women","s","national","volleyball","team","and","played","for","injs","yaound","in","she","was","part","of","the","cameroonian","national","team","at","the","fivb","volleyball","women","s","world","championship","in","italy","out","of","the","closet","is","a","nonprofit","chain","of","thrift","stores","whose","revenues","provide","medical","care","for","patients","with","hiv","aids","the","chain","is","owned","and","operated","by","the","aids","healthcare","foundation","ahf","a","los","angeles","based","charity","that","provides","medical","preventative","and","educational","resources","for","patients","ahf","is","the","nation","s","largest","non","profit","hiv","aids","healthcare","research","prevention","and","education","provider","out","of","the","closet","thrift","stores","generate","income","to","help","fund","the","medical","services","ahf","provides","for","those","patients","who","are","unable","to","pay","out","of","the","closet","was","founded","by","ahf","president","and","co","founder","michael","weinstein","whose","retail","experience","stemmed","from","his","family","s","furniture","business","on","the","east","coast","he","opened","the","first","out","of","the","closet","in","atwater","village","in","in","addition","to","out","of","the","closet","s","retail","activities","the","stores","provide","customers","with","a","variety","of","opportunities","to","donate","to","charity","the","store","s","merchandise","comes","directly","from","donations","and","therefore","the","store","s","revenues","are","directly","reliant","upon","neighborhood","charity","there","are","now","out","of","the","closet","locations","throughout","southern","california","four","locations","in","the","san","francisco","bay","area","and","four","locations","in","south","florida","two","in","miami","and","two","in","fort","lauderdale","the","out","of","the","closet","name","has","been","federally","trademarked","by","ahf","since","in","addition","to","regular","thrift","store","operations","several","stores","also","offer","additional","services","including","free","rapid","std","and","hiv","testing","on","a","walk","in","basis","along","with","counseling","in","a","separate","location","of","the","store","steals","and","deals","was","an","evening","business","news","talk","show","aired","weekdays","from","to","pm","et","on","cnbc","from","until","c","hosted","by","janice","lieberman","produced","by","glenn","ruppel","steals","and","deals","was","cnbc","s","nightly","investigative","consumer","finance","show","the","show","s","tagline","was","if","it","sounds","too","good","to","be","true","it","probably","is","steals","and","deals","at","http","www","stealsanddeals","com","is","a","discount","shopping","website","it","is","the","first","and","original","steals","and","deals","website","online","since","thomas","simart","born","october","is","a","french","sprint","canoeist","who","has","competed","since","the","late","s","he","won","a","two","medals","at","the","icf","canoe","sprint","world","championships","with","a","silver","c","and","a","bronze","c","x","m","the","lane","cove","river","a","northern","tributary","of","the","parramatta","river",
            "is","a","tide","dominated","drowned","valley","estuary","west","of","sydney","harbour","located","in","sydney","new","south","wales","australia","the","river","is","a","tributary","of","the","parramatta","river","winding","through","a","bushland","valley","the","lane","cove","river","rises","near","thornleigh","and","flows","generally","south","for","about","kilometres","mi","its","catchment","area","is","approximately","square","kilometres","sq","mi","the","upper","reaches","are","in","a","narrow","forested","valley","eroded","into","the","north","shore","plateau","the","middle","reaches","are","impounded","by","a","weir","just","upstream","of","fullers","bridge","sections","of","the","valley","are","forested","and","are","protected","within","the","lane","cove","national","park","an","area","of","hectares","acres","formerly","a","state","recreation","area","the","confluence","of","the","river","is","with","scout","creek","in","lane","cove","national","park","at","cheltenham","at","north","epping","still","within","the","confines","of","the","national","park","it","is","joined","by","devlins","creek","from","the","south","and","terrys","creek","near","macquarie","park","south","west","of","killara","and","lindfield","the","width","of","the","river","expands","continuing","south","through","lane","cove","river","national","park","towards","the","suburbs","of","linley","point","and","riverview","before","finally","reaching","its","mouth","between","greenwich","point","and","woolwich","where","it","merges","with","parramatta","river","and","soon","after","becomes","part","of","port","jackson","more","commonly","known","as","sydney","harbour","devlin","creek","was","named","after","the","devlin","family","who","lived","in","willandra","a","historical","house","in","ryde","the","area","surrounding","the","river","no","more","than","kilometre","mi","wide","is","called","lane","cove","national","park","and","is","a","site","of","ecological",
            "importance","listed","on","the","australian","register","of","the","national","estate","it","contains","an","endangered","community","of","fungi","some","species","of","which","have","still","not","been","classified","a","popular","caravan","park","and","campground","known","as","lane","cove","river","tourist","park","is","located","on","the","western","side","of","the","valley","above","the","river","the","lane","cove","river","is","the","site","of","many","old","trails","and","tracks","some","of","which","have","survived","from","logging","days","they","are","now","used","for","recreational","purposes","some","of","them","have","been","incorporated","into","the","great","north","walk","a","long","distance","walking","trail","from","sydney","to","newcastle","this","trail","passes","along","the","lane","cove","river","between","boronia","avenue","hunters","hill","and","thornleigh","oval","thornleigh","on","the","east","side","of","thornleigh","oval","the","trail","makes","use","of","lorna","pass","a","track","built","during","the","depression","of","the","s","to","provide","relief","work","from","to","the","early","s","the","swan","family","operated","a","picnic","area","called","fairyland","which","was","located","on","the","banks","of","the","river","upstream","from","epping","road","the","area","was","originally","a","market","garden","but","the","family","turned","it","into","a","picnic","area","when","they","realized","the","commercial","potential","facilities","were","developed","to","the","point","where","fairyland","had","its","own","footbridge","bbq","fireplaces","boat","swing","razzle","dazzle","ride","shelter","dance","hall","and","wharf","the","area","has","now","returned","to","nature","and","is","contained","within","the","lane","cove","national","park","harry","smith","was","a","businessman","who","owned","land","in","what","is","now","the","marsfield","area","smith","created","a","picnic","area","in","a","section","of","his","property","he","called","curzon","park","which","bordered","the","lane","cove","river","and","consisted","of","eighty","acres","of","bushland","the","picnic","area","has","long","since","returned","to","nature","but","a","set","of","stone","steps","can","still","be","seen","at","the","top","of","the","escarpment","above","the","river","it","is","amost","certain","that","smith","had","these","steps","built","to","provide","access","to","the","picnic","area","smith","also","had","a","quarry","in","the","area","near","the","present","location","of","talavera","road","from","which","he","obtained","the","stone","to","build","his","mansion","curzon","hall","the","latter","was","built","circa","and","is","located","at","the","intersection","of","balaclava","and","agincourt","roads","the","name","curzon","came","from","his","wife","s","name","isabella","curzon","webb","the","building","was","purchased","by","the","vincentian","fathers","in","and","turned","into","a","catholic","seminary","in","it","was","acquired","for","business","purposes","and","became","a","function","centre","steps","that","provided","access","to","harry","smith","s","picnic","area","lane","cove","river","at","fullers","bridge","chatswood","west","curzon","hall","archival","photo","of","fairyland","formation","known","as","whale","rock","outside","cheltenham","an","australian","brushturkey","in","the","national","park","steinunn","kristin","thordardottir","steinunn","krist","n","r","ard","ttir","born","april","in","reykjav","k","iceland","is","a","partner","at","beringer","finance","as","in","norway","previously","she","was","the","managing","director","of","glitnir","bank","in","london","uk","and","an","alternate","member","of","the","board","of","glitnir","bank","asa","in","norway","and","glitnir","sjodir","hf","in","iceland","steinunn","sits","on","the","board","of","the",
            "federico","pizarro","born","september","is","an","argentine","handball","player","for","unlu","and","the","argentine","national","team","he","competed","for","the","argentine","national","team","at","the","summer","olympics","in","london","he","participated","at","the","world","men","s","handball","championship","in","qatar","the","cm","nebelwerfer","cm","nbw","was","a","german","multiple","rocket","launcher","used","in","the","second","world","war","it","served","with","units","of","the","so","called","nebeltruppen","the","german","equivalent","of","the","u","the","cm","nbw","was","a","six","barrelled","rocket","launcher","mounted","on","a","two","wheeled","carriage","two","stabilizer","arms","and","a","spade","under","the","towing","ring","served","to","steady","the","carriage","while","firing","it","used","two","different","rockets","the","open","metal","frames","of","the","launcher","were","sized","to","fit","the","centimetres","in","rocket","but","adapter","rails","were","provided","to","allow","the","centimetres","in","rockets","to","fit","the","cm","wurfk","rper","spreng","explosive","missile","rocket","weighed","kilograms","lb","and","had","a","kilograms","lb","high","explosive","warhead","many","launchers","were","converted","from","to","use","the","new","cm","wurfk","rper","rockets","as","the","cm","nebelwerfer","the","and","cm","rockets","could","also","be","fired","from","their","packing","cases","packkiste","which","had","short","hinged","legs","to","adjust","elevation","they","could","also","be","mounted","in","groups","of","four","on","wooden","schwere","wurfger","t","heavy","missile","device","or","tubular","metal","schwere","wurfger","t","swg","launch","frames","the","external","dimensions","of","the","packing","cases","were","identical","so","no","adapter","was","needed","for","the","smaller","rockets","a","sequence","showing","swg","launchers","being","used","against","warsaw","during","the","warsaw","uprising","in","positioning","a","rocket","in","a","swg","firing","frame","the","rockets","have","just","been","fired","note","the","piles","of","used","packing","crates","not","all","the","rockets","have","been","fired","at","the","same","time","four","rockets","in","flight","the","cm","nbw","was","organized","into","batteries","of","six","launchers","with","three","batteries","per","battalion","these","battalions","were","concentrated","in","independent","werfer","regimenter","and","brigaden","the","bergen","corpus","of","london","teenage","language","colt","is","a","data","set","of","samples","of","spoken","english","that","was","compiled","in","from","tape","recorded","and","transcribed","conversations","by","teens","between","the","ages","of","and","in","london","schools","this","corpus","which","has","been","tagged","for","part","of","speech","using","the","claws","tagset","is","one","of","the","linguistic","research","projects","housed","at","the","university","of","bergen","in","norway","linguistic","analysis","based","on","this","corpus","material","has","appeared","in","the","book","trends","in","teenage","talk","and","later","journal","articles","volts","is","an","album","by","ac","dc","released","as","a","part","named","disc","four","on","the","bonfire","box","set","released","in","november","the","album","is","a","compilation","of","some","alternative","versions","of","songs","recorded","for","the","albums","let","there","be","rock","and","highway","to","hell","and","some","songs","previously","released","a","hidden","track","containing","various","interviews","appears","after","a","short","amount","of","silence","following","the","last","track","last","of","the","conquerors","is","the","debut","novel","by","african","american","journalist","and","editor","william","gardner","smith","the","novel","concerns","the","author","s","experience","as","an","african","american","gi","s","serving","in","the","racially","segregated","united","states","army","in","us","occupied","germany","after","world","war","ii","the","protagonist","hayes","dawkins","has","an","affair","with","ilse","a","white","german","woman","together","he","and","ilse","struggle","against","racist","army","officers","and","policies","to","sustain","a","relationship","that","some","white","soldiers","condemn","although","there","are","also","many","friendly","whites","who","help","them","last","of","the","conquerors","depicts","post","nazi","germany","as","more","racially","tolerant","than","the","united","states","wang","xueqin","born","january","in","jiangsu","is","a","chinese","long","distance","runner","she","competed","in","the","marathon","at","the","summer","olympics","placing","nd","with","a","time","of","nils","j","diaz","is","a","former","chairman","of","the","nuclear","regulatory","commission","dr","diaz","s","term","as","chairman","ended","on","june","he","was","a","nuclear","engineering","professor","and","chairman","at","the","university","of","florida","in","gainesville","florida","from","the","s","to","the","s","he","is","also","a","uf","alumnus","christopher","alder","was","a","trainee","computer","programmer","and","former","british","army","paratrooper","who","had","served","in","the","falklands","war","and","was","commended","for","his","service","with","the","army","in","northern","ireland","he","died","while","in","police","custody","at","queen","s","gardens","police","station","kingston","upon","hull","in","april","the","case","became","a","cause","c","l","bre","for","civil","rights","campaigners","in","the","united","kingdom","he","had","earlier","been","the","victim","of","an","assault","outside","a","nightclub","and","was","taken","to","hull","royal","infirmary","where","possibly","as","a","result","of","his","head","injury","staff","said","his","behaviour","was","extremely","troublesome","on","arrival","at","the","police","station","alder","was","partially","dragged","and","partially","carried","handcuffed","and","unconscious","from","a","police","van","and","placed","on","the","floor","of","the","custody","suite","officers","chatted","between","themselves","and","speculated","that","he","was","faking","illness","twelve","minutes","later","one","of","the","officers","present","noticed","that","alder","was","not","making","any","breathing","noises","and","although","resuscitation","was","attempted","he","was","pronounced","dead","at","the","scene","a","post","mortem","indicated","that","the","head","injury","alone","would","not","have","killed","him","a","coroner","s","jury","in","returned","a","verdict","that","alder","was","unlawfully","killed","in","five","police","officers","went","on","trial","charged","with","alder","s","manslaughter","and","misconduct","in","public","office","but","were","acquitted","on","the","orders","of","the","judge","in","an","independent","police","complaints","commission","report","concluded","that","four","of","the","officers","present","in","the","custody","suite","when","alder","died","were","guilty","of","the","most","serious","neglect","of","duty","in","november","the","government","formally","apologised","to","alder","s","family","in","the","european","court","of","human","rights","admitting","that","it","had","breached","its","obligations","with","regard","to","preserving","life","and","ensuring","no","one","is","subjected","to",
            "inhuman","or","degrading","treatment","christopher","ibikunle","alder","june","april","was","a","black","british","man","of","nigerian","descent","born","in","hull","in","he","joined","the","british","army","at","the","age","of","and","served","in","the","parachute","regiment","for","six","years","after","leaving","the","army","he","first","settled","in","andover","hampshire","before","relocating","to","dagger","lane","hull","in","in","he","was","taking","a","college","course","in","computer","skills","in","hull","he","had","two","sons","who","had","remained","with","their","mother","in","the","andover","area","when","their","parents","separated","at","around","pm","on","march","alder","went","out","for","the","evening","in","hull","with","two","friends","visiting","several","local","bars","and","a","fast","food","restaurant","before","alder","suggested","going","on","to",
            "the","waterfront","club","later","renamed","the","sugar","mill","a","nightclub","on","prince","s","dock","street","in","the","old","town","area","of","the","city","his","companions","who","later","testified","that","at","this","stage","of","the","evening","alder","had","drunk","only","two","pints","of","lager","and","two","bottles","of","beck","s","beer","and","seemed","sober","declined","the","invitation","two","police","officers","pc","nigel","dawson","and","pc","neil","blakey","who","had","arrived","shortly","after","the","ambulance","in","a","marked","patrol","car","made","no","attempt","to","speak","to","alder","they","consulted","with","the","club","s","manager","who","took","them","inside","to","review","the","club","s","cctv","footage","of","the","incident","a","message","they","sent","to","their","control","room","at","this","time","indicates","that","they","had","already","assumed","alder","was","very","drunk","despite","not","having","spoken","to","him","or","having","been","told","this","by","any","of","the","witnesses","they","spoke","with","the","ambulance","arrived","at","the","hospital","at","am","where","alder","was","described","by","one","witness","who","dealt","with","him","as","confused","and","dazed","and","generally","abusive","one","of","the","paramedics","from","the","ambulance","crew","who","had","transported","him","there","stated","that","alder","asked","where","am","i","what","s","happened","one","of","the","nurses","who","treated","him","also","stated","that","in","addition","to","being","abusive","and","swearing","at","her","he","was","asking","where","am",
            "i","what","am","i","doing","two","police","officers","who","were","present","in","the","emergency","department","on","an","unrelated","matter","intervened","at","one","point","and","asked","him","to","cooperate","with","the","nursing","staff","gornja","suvaja","cyrillic","is","a","village","in","the","municipality","of","bosanska","krupa","bosnia","and","herzegovina","coordinates","n","e","n","e","henriette","koulla","born","september","is","a","cameroonian","female","volleyball","player","she","is","a","member","of","the","cameroon","women","s","national","volleyball","team","and","played","for","injs","yaound","in","she","was","part","of","the","cameroonian","national","team","at","the","fivb","volleyball","women","s","world","championship","in","italy","out","of","the","closet","is","a","nonprofit","chain","of","thrift","stores","whose","revenues","provide","medical","care","for","patients","with","hiv","aids","the","chain","is","owned","and","operated","by","the","aids","healthcare","foundation","ahf","a","los","angeles","based","charity","that","provides","medical","preventative","and","educational","resources","for","patients","ahf","is","the","nation","s","largest","non","profit","hiv","aids","healthcare","research","prevention","and","education","provider","out","of","the","closet","thrift","stores","generate","income","to","help","fund","the","medical","services","ahf","provides","for","those","patients","who","are","unable","to","pay","out","of","the","closet","was","founded","by","ahf","president","and","co","founder","michael","weinstein","whose","retail","experience","stemmed","from","his","family","s","furniture","business","on","the","east","coast","he","opened","the","first","out","of","the","closet","in","atwater","village","in","in","addition","to","out","of","the","closet","s","retail","activities","the","stores","provide","customers","with","a","variety","of","opportunities","to","donate","to","charity","the","store","s","merchandise","comes","directly","from","donations","and","therefore","the","store","s","revenues","are","directly","reliant","upon","neighborhood","charity","there","are","now","out","of","the","closet","locations","throughout","southern","california","four","locations","in","the","san","francisco","bay","area","and","four","locations","in","south","florida","two","in","miami","and","two","in","fort","lauderdale","the","out","of","the","closet","name","has","been","federally","trademarked","by","ahf","since","in","addition","to","regular","thrift","store","operations","several","stores","also","offer","additional","services","including","free","rapid","std","and","hiv","testing","on","a","walk","in","basis","along","with","counseling","in","a","separate","location","of","the","store","steals","and","deals","was","an","evening","business","news","talk","show","aired","weekdays","from","to","pm","et","on","cnbc","from","until","c","hosted","by","janice","lieberman","produced","by","glenn","ruppel","steals","and","deals","was","cnbc","s","nightly","investigative","consumer","finance","show","the","show","s","tagline","was","if","it","sounds","too","good","to","be","true","it","probably","is","steals","and","deals","at","http","www","stealsanddeals","com","is","a","discount","shopping","website","it","is","the","first","and","original","steals","and","deals","website","online","since","thomas","simart","born","october","is","a","french","sprint","canoeist","who","has","competed","since","the","late","s","he","won","a","two","medals","at","the","icf","canoe","sprint","world","championships","with","a","silver","c","and","a","bronze","c","x","m","the","lane","cove","river","a","northern","tributary","of","the","parramatta","river",
            "is","a","tide","dominated","drowned","valley","estuary","west","of","sydney","harbour","located","in","sydney","new","south","wales","australia","the","river","is","a","tributary","of","the","parramatta","river","winding","through","a","bushland","valley","the","lane","cove","river","rises","near","thornleigh","and","flows","generally","south","for","about","kilometres","mi","its","catchment","area","is","approximately","square","kilometres","sq","mi","the","upper","reaches","are","in","a","narrow","forested","valley","eroded","into","the","north","shore","plateau","the","middle","reaches","are","impounded","by","a","weir","just","upstream","of","fullers","bridge","sections","of","the","valley","are","forested","and","are","protected","within","the","lane","cove","national","park","an","area","of","hectares","acres","formerly","a","state","recreation","area","the","confluence","of","the","river","is","with","scout","creek","in","lane","cove","national","park","at","cheltenham","at","north","epping","still","within","the","confines","of","the","national","park","it","is","joined","by","devlins","creek","from","the","south","and","terrys","creek","near","macquarie","park","south","west","of","killara","and","lindfield","the","width","of","the","river","expands","continuing","south","through","lane","cove","river","national","park","towards","the","suburbs","of","linley","point","and","riverview","before","finally","reaching","its","mouth","between","greenwich","point","and","woolwich","where","it","merges","with","parramatta","river","and","soon","after","becomes","part","of","port","jackson","more","commonly","known","as","sydney","harbour","devlin","creek","was","named","after","the","devlin","family","who","lived","in","willandra","a","historical","house","in","ryde","the","area","surrounding","the","river","no","more","than","kilometre","mi","wide","is","called","lane","cove","national","park","and","is","a","site","of","ecological",
            "importance","listed","on","the","australian","register","of","the","national","estate","it","contains","an","endangered","community","of","fungi","some","species","of","which","have","still","not","been","classified","a","popular","caravan","park","and","campground","known","as","lane","cove","river","tourist","park","is","located","on","the","western","side","of","the","valley","above","the","river","the","lane","cove","river","is","the","site","of","many","old","trails","and","tracks","some","of","which","have","survived","from","logging","days","they","are","now","used","for","recreational","purposes","some","of","them","have","been","incorporated","into","the","great","north","walk","a","long","distance","walking","trail","from","sydney","to","newcastle","this","trail","passes","along","the","lane","cove","river","between","boronia","avenue","hunters","hill","and","thornleigh","oval","thornleigh","on","the","east","side","of","thornleigh","oval","the","trail","makes","use","of","lorna","pass","a","track","built","during","the","depression","of","the","s","to","provide","relief","work","from","to","the","early","s","the","swan","family","operated","a","picnic","area","called","fairyland","which","was","located","on","the","banks","of","the","river","upstream","from","epping","road","the","area","was","originally","a","market","garden","but","the","family","turned","it","into","a","picnic","area","when","they","realized","the","commercial","potential","facilities","were","developed","to","the","point","where","fairyland","had","its","own","footbridge","bbq","fireplaces","boat","swing","razzle","dazzle","ride","shelter","dance","hall","and","wharf","the","area","has","now","returned","to","nature","and","is","contained","within","the","lane","cove","national","park","harry","smith","was","a","businessman","who","owned","land","in","what","is","now","the","marsfield","area","smith","created","a","picnic","area","in","a","section","of","his","property","he","called","curzon","park","which","bordered","the","lane","cove","river","and","consisted","of","eighty","acres","of","bushland","the","picnic","area","has","long","since","returned","to","nature","but","a","set","of","stone","steps","can","still","be","seen","at","the","top","of","the","escarpment","above","the","river","it","is","amost","certain","that","smith","had","these","steps","built","to","provide","access","to","the","picnic","area","smith","also","had","a","quarry","in","the","area","near","the","present","location","of","talavera","road","from","which","he","obtained","the","stone","to","build","his","mansion","curzon","hall","the","latter","was","built","circa","and","is","located","at","the","intersection","of","balaclava","and","agincourt","roads","the","name","curzon","came","from","his","wife","s","name","isabella","curzon","webb","the","building","was","purchased","by","the","vincentian","fathers","in","and","turned","into","a","catholic","seminary","in","it","was","acquired","for","business","purposes","and","became","a","function","centre","steps","that","provided","access","to","harry","smith","s","picnic","area","lane","cove","river","at","fullers","bridge","chatswood","west","curzon","hall","archival","photo","of","fairyland","formation","known","as","whale","rock","outside","cheltenham","an","australian","brushturkey","in","the","national","park","steinunn","kristin","thordardottir","steinunn","krist","n","r","ard","ttir","born","april","in","reykjav","k","iceland","is","a","partner","at","beringer","finance","as","in","norway","previously","she","was","the","managing","director","of","glitnir","bank","in","london","uk","and","an","alternate","member","of","the","board","of","glitnir","bank","asa","in","norway","and","glitnir","sjodir","hf","in","iceland","steinunn","sits","on","the","board","of","the"

        };
        private static char[][] _testWordsChars = _testWords.Select(s => s.ToCharArray()).ToArray();

        private static HashSet<string> _stopWordsStringSet;
        private static long[] _stopWordsLongs;
        private static HashSet<long> _stopWordsLongHashSet;
        private static StopWordSet _stopWordsCustomSet;
        private static SortedSet<long> _stopWordsLongSortedSet;


        [Params(5, 10, 20, 30, 40, 50)]
        public int StopWordCount;

        [GlobalSetup]
        public void Setup()
        {
            _stopWordsStringSet = new HashSet<string>(_stopWords.Take(StopWordCount));
            _stopWordsLongs = StringToLong(_stopWords.Take(StopWordCount).ToArray());

            _stopWordsLongHashSet = new HashSet<long>(_stopWordsLongs);
            _stopWordsLongSortedSet = new SortedSet<long>(_stopWordsLongs);
            _stopWordsCustomSet = new StopWordSet(_stopWordsLongs);
        }

        //[Benchmark]
        //public int StringIteration()
        //{
        //    var wordCount = 0;
        //    for (var i = 0; i < _testWords.Length; i++)
        //    {
        //        for (var t = 0; t < _stopWords.Length; t++)
        //        {
        //            if (String.Compare(_testWords[i], _stopWords[t], true, CultureInfo.InvariantCulture) == 0)
        //            {
        //                wordCount += 1;
        //                break;
        //            }
        //        }
        //    }
        //    return wordCount;
        //}

        [Benchmark(Baseline = true, OperationsPerInvoke = 5000)]
        public int StringSet()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_stopWordsStringSet.Contains(_testWords[i]))
                {
                    wordCount += 1;
                }
            }
            return wordCount;
        }

        [Benchmark(OperationsPerInvoke = 5000)]
        public unsafe int LongIteration()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_testWordsChars[i].Length > 4)
                {
                    continue;
                }

                long checkValue;
                fixed (char* word = _testWordsChars[i])
                {
                    checkValue = *(long*)word;
                }

                for (var t = 0; t < _stopWordsLongs.Length; t++)
                {
                    if (_stopWordsLongs[t] - checkValue == 0)
                    {
                        wordCount += 1;
                        break;
                    }
                }
            }

            return wordCount;

        }

        [Benchmark(OperationsPerInvoke = 5000)]
        public unsafe int LongHashSet()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_testWordsChars[i].Length > 4)
                {
                    continue;
                }

                long checkValue;
                fixed (char* word = _testWordsChars[i])
                {
                    checkValue = *(long*)word;
                }

                if (_stopWordsLongHashSet.Contains(checkValue))
                {
                    wordCount += 1;
                }
            }
            return wordCount;
        }

        [Benchmark(OperationsPerInvoke = 5000)]
        public unsafe int LongBinarySearch()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_testWordsChars[i].Length > 4)
                {
                    continue;
                }

                long checkValue;
                fixed (char* word = _testWordsChars[i])
                {
                    checkValue = *(long*)word;
                }

                if (Array.BinarySearch(_stopWordsLongs, checkValue) > -1)
                {
                    wordCount += 1;
                }
            }
            return wordCount;
        }

        [Benchmark(OperationsPerInvoke = 5000)]
        public unsafe int LongTreeSet()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_testWordsChars[i].Length > 4)
                {
                    continue;
                }

                long checkValue;
                fixed (char* word = _testWordsChars[i])
                {
                    checkValue = *(long*)word;
                }

                if (_stopWordsLongSortedSet.Contains(checkValue))
                {
                    wordCount += 1;
                }
            }
            return wordCount;
        }

        [Benchmark(OperationsPerInvoke = 5000)]
        public unsafe int StopWordSet()
        {
            var wordCount = 0;
            for (var i = 0; i < _testWords.Length; i++)
            {
                if (_testWordsChars[i].Length > 4)
                {
                    continue;
                }

                long checkValue;
                fixed (char* word = _testWordsChars[i])
                {
                    checkValue = *(long*)word;
                }

                if (_stopWordsCustomSet.Contains(checkValue))
                {
                    wordCount += 1;
                }
            }
            return wordCount;
        }

        private static unsafe long[] StringToLong(string[] strings)
        {
            var list = new long[strings.Length];
            for (var i = 0; i < strings.Length; i++)
            {
                var s = strings[i];
                var chars = s.ToCharArray();
                if (chars.Length > 4)
                {
                    throw new Exception("Can not use strings > 4 ");
                }
                fixed (char* c = chars)
                {
                    var lp = (long*)c;
                    list[i] = *lp;
                }
            }

            return list.OrderBy(l => l).ToArray();
        }

    }

    public class Program
    {
        static void Main(string[] args)
        {
            // benchmark
            var summary = BenchmarkRunner.Run<StopWordChecker>();

            // simple test - just to make sure they all find the same number of stopwords 
            var instance = new StopWordChecker();

            var r1 = instance.StringSet();
            var r2 = instance.LongIteration();
            var r3 = instance.LongBinarySearch();
            var r4 = instance.LongTreeSet();
            var r5 = instance.LongHashSet();
            var r6 = instance.StopWordSet();

            if (r1 != r2 || r2 != r3 || r3 != r4 || r4 != r5 || r5 != r6)
            {
                Console.WriteLine($"[ERROR] Return values do not match! {r1},{r2},{r3},{r4},{r5},{r6}");
            }
        }
    }
}
