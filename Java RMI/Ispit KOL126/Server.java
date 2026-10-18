import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(5099);
            } catch (RemoteException e) {
            }

            StockService service = new StockServiceImpl();
            Naming.rebind("rmi://localhost:5099/StockService", service);
            System.out.println("StockService je pokrenut na portu 5099.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
