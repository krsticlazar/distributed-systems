import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

/**
 * Serverska ulazna tacka za eBank sistem.
 *
 * Server objavljuje udaljeni objekat pod imenom "EBanka" kako bi klijent
 * kasnije mogao da ga pronadje.
 */
public class Server {
    public Server() {
        try {
            try {
                // Kreiramo registry ako vec ne postoji.
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
                // U redu je ako registry vec postoji.
            }

            Naming.rebind("EBanka", new EBankaImpl());
            System.out.println("eBanka server je pokrenut.");
            System.out.println("Pritisnite Enter za gasenje servera.");
            // Server ostaje aktivan dok korisnik ne potvrdi gasenje.
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public static void main(String[] args) {
        new Server();
    }
}
