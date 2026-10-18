import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.Arrays;

public class Klijent {
    private final Broker broker;

    public Klijent(String host) throws Exception {
        broker = (Broker) Naming.lookup("rmi://" + host + ":1099/Broker");
    }

    public void subscribe(String topic) throws Exception {
        MqttClientImpl callback = new MqttClientImpl();

        try {
            broker.subscribe(topic, callback);
            System.out.println("Pretplacen si na topic '" + topic + "'. Pritisni Enter za kraj.");
            System.in.read();
        } finally {
            UnicastRemoteObject.unexportObject(callback, true);
        }
    }

    public void publish(String topic, String naslov, String sadrzaj) throws Exception {
        broker.publish(topic, new Poruka(naslov, sadrzaj));
        System.out.println("Poruka je poslata.");
    }

    private static void ispisiUpotrebu() {
        System.out.println("Upotreba:");
        System.out.println("java Klijent sub TOPIC");
        System.out.println("java Klijent pub TOPIC NASLOV SADRZAJ");
        System.out.println("java Klijent IP_ADRESA_SERVERA sub TOPIC");
        System.out.println("java Klijent IP_ADRESA_SERVERA pub TOPIC NASLOV SADRZAJ");
    }

    public static void main(String[] args) {
        try {
            if (args.length < 2) {
                ispisiUpotrebu();
                return;
            }

            int indeksKomande = 0;
            String host = "localhost";

            if (!args[0].equalsIgnoreCase("sub") && !args[0].equalsIgnoreCase("pub")) {
                host = args[0];
                indeksKomande = 1;
            }

            if (args.length <= indeksKomande + 1) {
                ispisiUpotrebu();
                return;
            }

            Klijent klijent = new Klijent(host);
            String komanda = args[indeksKomande];
            String topic = args[indeksKomande + 1];

            if (komanda.equalsIgnoreCase("sub")) {
                klijent.subscribe(topic);
            } else if (komanda.equalsIgnoreCase("pub")) {
                if (args.length <= indeksKomande + 3) {
                    ispisiUpotrebu();
                    return;
                }

                String naslov = args[indeksKomande + 2];
                String sadrzaj = String.join(" ", Arrays.copyOfRange(args, indeksKomande + 3, args.length));
                klijent.publish(topic, naslov, sadrzaj);
            } else {
                ispisiUpotrebu();
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    private static class MqttClientImpl extends UnicastRemoteObject implements MqttClient {
        public MqttClientImpl() throws RemoteException {
            super();
        }

        public void primiPoruku(String topic, Poruka poruka) throws RemoteException {
            System.out.println("Primljena poruka [" + topic + "]: " + poruka);
        }
    }
}
