import java.rmi.Remote;
import java.rmi.RemoteException;

public interface Broker extends Remote {
    void subscribe(String topic, MqttClient klijent) throws RemoteException;
    void publish(String topic, Poruka poruka) throws RemoteException;
}
