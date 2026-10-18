import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(4096);
            } catch (RemoteException e) {
            }

            FootballScore footballScore = new FootballScoreImpl();
            Naming.rebind("rmi://localhost:4096/FootballScore", footballScore);
            System.out.println("KOL124 FootballScore server je pokrenut na portu 4096.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
