import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(3099);
            } catch (RemoteException e) {
            }

            QueueService service = new QueueServiceImpl();
            Naming.rebind("rmi://localhost:3099/QueueService", service);
            System.out.println("Sistem reda cekanja je pokrenut na portu 3099.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
