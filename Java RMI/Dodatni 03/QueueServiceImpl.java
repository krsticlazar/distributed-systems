import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;
import java.util.HashMap;
import java.util.LinkedList;
import java.util.Map;
import java.util.Queue;

public class QueueServiceImpl extends UnicastRemoteObject implements QueueService {
    private final Queue<Ticket> queue = new LinkedList<>();
    private final Map<Integer, QueueCallback> callbacks = new HashMap<>();
    private int nextNumber = 1;

    public QueueServiceImpl() throws RemoteException {
        super();
    }

    public synchronized Ticket takeTicket(String clientName, QueueCallback callback)
            throws RemoteException {
        Ticket ticket = new Ticket(nextNumber++, clientName);
        queue.offer(ticket);
        callbacks.put(ticket.getNumber(), callback);
        return ticket;
    }

    public synchronized String getQueue() throws RemoteException {
        return queue.toString();
    }

    public void callNext() throws RemoteException {
        Ticket ticket;
        QueueCallback callback;

        synchronized (this) {
            ticket = queue.poll();

            if (ticket == null) {
                return;
            }

            callback = callbacks.remove(ticket.getNumber());
        }

        if (callback != null) {
            callback.ticketCalled(ticket.getNumber());
        }
    }
}
