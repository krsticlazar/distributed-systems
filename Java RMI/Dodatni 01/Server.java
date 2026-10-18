import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
            }

            Glasanje glasanje = new GlasanjeImpl();
            Naming.rebind("rmi://localhost:1099/Glasanje", glasanje);
            System.out.println("Servis za glasanje je pokrenut.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
