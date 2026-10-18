import java.rmi.Naming;

public class AdminClient {
    public static void main(String[] args) {
        try {
            String host = args.length > 0 ? args[0] : "localhost";
            QueueService service = (QueueService) Naming.lookup(
                    "rmi://" + host + ":3099/QueueService");

            service.callNext();
            System.out.println("Pozvan je sledeci klijent.");
            System.out.println("Preostali red: " + service.getQueue());
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
