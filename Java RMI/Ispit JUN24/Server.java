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

            Broker broker = new BrokerImpl();
            Naming.rebind("rmi://localhost:1099/Broker", broker);
            System.out.println("JUN24 MQTT broker je pokrenut.");
            System.in.read();
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
