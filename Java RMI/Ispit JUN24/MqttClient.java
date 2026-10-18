import java.rmi.Remote;
import java.rmi.RemoteException;

public interface MqttClient extends Remote {
    void primiPoruku(String topic, Poruka poruka) throws RemoteException;
}
