namespace Lapine;

using System.Threading.Tasks.Sources;

interface IMailbox
{
    ValueTask<Object> Receive();
    void Post(Object message);
}

class MailboxProcessor
{
    readonly IMailbox _mailbox = new Mailbox();
    readonly Func<IMailbox, Task> _body;

    public MailboxProcessor(Func<IMailbox, Task> body) =>
        _body = body ?? throw new ArgumentNullException(nameof(body));

    void Start()
    {
        Task.Factory.StartNew(() => _body(_mailbox));
    }

    void Post(Object message) =>
        _mailbox.Post(message);

    static public MailboxProcessor StartNew(Func<IMailbox, Task> body)
    {
        var mailboxProcessor = new MailboxProcessor(body);
        mailboxProcessor.Start();


        MailboxProcessor.StartNew(async (mailbox) =>
        {
            while (true)
            {
                switch (await mailbox.Receive())
                {
                    case (":start", Int32 foo):
                    {
                        break;
                    }
                }
            }
        });

        return mailboxProcessor;
    }
}

class Mailbox : IMailbox, IValueTaskSource<Object>
{
    Queue<Object> _queue;
    ManualResetValueTaskSourceCore<Object> _valueTaskSource;
    Boolean _signalled, _activeWait;

    public Mailbox()
    {
        _queue = new Queue<Object>();
        _valueTaskSource = new ManualResetValueTaskSourceCore<Object>();
    }

    public ValueTask<Object> Receive()
    {
        lock (this)
        {
            if (_activeWait)
                throw new Exception("Only one consumer should be waiting for messages at any time");

            if (_signalled)
            {
                _signalled = false;
                return new ValueTask<Object>(_queue.Dequeue());
            }

            _activeWait = true;
            return new ValueTask<Object>(this, _valueTaskSource.Version);
        }
    }

    public void Post(Object message)
    {
        lock (this)
        {
            _signalled = true;

            if (_activeWait)
            {
                _valueTaskSource.SetResult(message);
            }
            else
            {
                _queue.Enqueue(message);
            }
        }
    }

    Object IValueTaskSource<Object>.GetResult(Int16 token)
    {
        lock (this)
        {
            try
            {
                return _valueTaskSource.GetResult(token);
            }
            finally
            {
                _valueTaskSource.Reset();
                _activeWait = false;
                _signalled = false;
            }
        }
    }

    ValueTaskSourceStatus IValueTaskSource<Object>.GetStatus(Int16 token) =>
        _valueTaskSource.GetStatus(token);

    void IValueTaskSource<Object>.OnCompleted(Action<Object?> continuation, Object? state, Int16 token, ValueTaskSourceOnCompletedFlags flags) =>
        _valueTaskSource.OnCompleted(continuation, state, token, flags);
}
