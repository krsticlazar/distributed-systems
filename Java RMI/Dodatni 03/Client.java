import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class Client {
    public static void main(String[] args) {
        QueueCallbackImpl callback = null;

        try {
            String host = args.length > 0 ? args[0] : "localhost";
            String clientName = args.length > 1 ? args[1] : "Marko";

            QueueService service = (QueueService) Naming.lookup(
                    "rmi://" + host + ":3099/QueueService");

            callback = new QueueCallbackImpl();
            Ticket ticket = service.takeTicket(clientName, callback);
            System.out.println("Dobijen tiket: " + ticket);
            System.out.println("Trenutni red: " + service.getQueue());
            System.out.println("Klijent ceka prozivku. Pritisni Enter za kraj.");

            if (args.length > 2) {
                Thread.sleep(Long.parseLong(args[2]));
            } else {
                System.in.read();
            }
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            if (callback != null) {
                try {
                    UnicastRemoteObject.unexportObject(callback, true);
                } catch (Exception e) {
                }
            }
        }
    }

    private static class QueueCallbackImpl extends UnicastRemoteObject
            implements QueueCallback {
        public QueueCallbackImpl() throws RemoteException {
            super();
        }

        public void ticketCalled(int number) throws RemoteException {
            System.out.println("Prozvan je tiket " + number + ".");
        }
    }
}
