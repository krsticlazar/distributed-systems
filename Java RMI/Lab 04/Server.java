import java.rmi.*;
import java.rmi.registry.*;

public class Server {
    public Server() {
        try {
            try {
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
            }

            EStudSluzba sluzba = new EStudSluzbaImpl();
            Naming.rebind("rmi://localhost:1099/EStudSluzba", sluzba);
            System.out.println("Server studentske sluzbe je pokrenut.");
            System.in.read();
        } catch (Exception e) {
        }
    }

    public static void main(String[] args) {
        new Server();
    }
}
