import java.rmi.*;
import java.rmi.registry.*;

public class Server {
    public Server() {
        try {
            try {
                LocateRegistry.createRegistry(1099);
            } catch (RemoteException e) {
            }

            Generator generator = new GeneratorImpl();
            Naming.rebind("rmi://localhost:1099/Generator", generator);
            System.out.println("APR26 server je pokrenut.");
            System.in.read();
        } catch (Exception e) {
        }
    }

    public static void main(String[] args) {
        new Server();
    }
}
