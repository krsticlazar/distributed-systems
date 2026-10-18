import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.registry.LocateRegistry;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(2099);
            } catch (RemoteException e) {
            }

            Library library = new LibraryImpl();
            Naming.rebind("rmi://localhost:2099/Library", library);
            System.out.println("Digitalna biblioteka je pokrenuta na portu 2099.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
