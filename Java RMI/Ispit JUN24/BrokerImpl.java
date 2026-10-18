import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

public class BrokerImpl extends UnicastRemoteObject implements Broker {
    private final Map<String, List<MqttClient>> topici = new HashMap<>();

    public BrokerImpl() throws RemoteException {
        super();
    }

    private void createTopic(String topic) {
        topici.putIfAbsent(topic, new ArrayList<>());
    }

    public synchronized void subscribe(String topic, MqttClient klijent) throws RemoteException {
        createTopic(topic);
        topici.get(topic).add(klijent);
        System.out.println("Novi klijent je pretplacen na topic: " + topic);
    }

    public void publish(String topic, Poruka poruka) throws RemoteException {
        List<MqttClient> pretplaceni;

        synchronized (this) {
            createTopic(topic);
            pretplaceni = new ArrayList<>(topici.get(topic));
        }

        System.out.println("Poruka na topicu " + topic + ": " + poruka);
        obavestiPretplacene(topic, poruka, pretplaceni);
    }

    private void obavestiPretplacene(String topic, Poruka poruka, List<MqttClient> pretplaceni) {
        List<MqttClient> neaktivni = new ArrayList<>();

        for (MqttClient klijent : pretplaceni) {
            try {
                klijent.primiPoruku(topic, poruka);
            } catch (RemoteException e) {
                neaktivni.add(klijent);
            }
        }

        synchronized (this) {
            topici.get(topic).removeAll(neaktivni);
        }
    }
}
