import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public Server() {
        try {
            try {
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
            }

            Naming.rebind("rmi://localhost:1099/Kviz", new KvizImpl());
            System.out.println("Kviz server je pokrenut.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    public static void main(String[] args) {
        new Server();
    }
}
