import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class ClientUser {
    public static void main(String[] args) {
        CallbackImpl callback = null;

        try {
            String host = args.length > 0 ? args[0] : "localhost";
            FootballScore footballScore = (FootballScore) Naming.lookup("rmi://" + host + ":4096/FootballScore");

            System.out.println("Sve utakmice:");
            System.out.println(footballScore.getAllResults());

            Match utakmica2 = footballScore.getMatch(2);
            System.out.println("Stadion utakmice 2: " + utakmica2.getStadium().getName());

            Match utakmica1 = footballScore.getMatch(1);
            callback = new CallbackImpl(footballScore);
            utakmica1.subscribe(callback);

            System.out.println("Pretplacen si na promenu rezultata utakmice 1.");
            System.out.println("Pokreni ClientAdmin u drugom terminalu. Pritisni Enter za kraj.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            if (callback != null) {
                try {
                    UnicastRemoteObject.unexportObject(callback, true);
                } catch (Exception e) {
                }
            }
        }
    }

    private static class CallbackImpl extends UnicastRemoteObject implements Callback {
        private final FootballScore footballScore;

        public CallbackImpl(FootballScore footballScore) throws RemoteException {
            super();
            this.footballScore = footballScore;
        }

        public void resultChanged(int matchId) throws RemoteException {
            Match match = footballScore.getMatch(matchId);
            System.out.println("Promena rezultata: " + match.getResult());
        }
    }
}
