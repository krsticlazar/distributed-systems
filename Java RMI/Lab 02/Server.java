import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

/**
 * Serverska ulazna tacka za aplikaciju mobilnog operatera.
 *
 * Server pravi registry, kreira objekat tipa {@code OperaterImpl} i registruje
 * ga pod imenom "Operater".
 */
public class Server {
    public Server() {
        try {
            try {
                // Standardni RMI port za laboratorijske primere.
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
                // Registry mozda vec postoji ako je server ranije pokrenut.
            }

            Naming.rebind("Operater", new OperaterImpl());
            System.out.println("Mobilni operater server je pokrenut.");
            System.out.println("Pritisnite Enter za gasenje servera.");
            // Blokiramo glavni thread da server ne bi odmah zavrsio rad.
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public static void main(String[] args) {
        new Server();
    }
}
