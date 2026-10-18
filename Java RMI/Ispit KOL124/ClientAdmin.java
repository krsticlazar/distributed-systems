import java.rmi.Naming;

public class ClientAdmin {
    public static void main(String[] args) {
        try {
            String host = args.length > 0 ? args[0] : "localhost";
            FootballScore footballScore = (FootballScore) Naming.lookup("rmi://" + host + ":4096/FootballScore");
            Match utakmica1 = footballScore.getMatch(1);

            utakmica1.addHomeGoal();
            System.out.println("Dodat je gol domacem timu na utakmici 1.");
            System.out.println(utakmica1.getResult());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
