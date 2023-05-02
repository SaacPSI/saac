using System;
using Microsoft.Psi;
using Microsoft.Psi.Components;

public class Component: Subpipeline
{
    // Connector for the reciever
    protected Connector<TYPE_IN> InConnector;
    // Reciever that encapsulates the input.
     public Receiver<TYPE_IN> In => InConnector. In;
    // Emitter taht encapsulates the output.
    public Emitter<TYPE_OUT> Out { get; private set; }
    // TODO: Add more/modify stuff if needed
    // Constructor
    public Component(Pipeline pipeline, string name = "Component", DeliveryPolicy policy = null)
        : base(parent, name, policy)
    { 
        // Connections creations
        InConnector = pipeline.CreateConnector<TYPE_IN>(nameof(InConnector));
        Out = pipeline.CreateEmitter<TYPE_OUT>(this, nameof(Out));
    }
    // Registering method that will be called for every input InConnector.Out.Do (Process);
    private void Process (TYPE_IN data, Envelope envelope)
    {
        //TO DO process
        lock (this)
        {
            //If out:
            Out.Post(TYPE_OUT, DateTime.Now);
        }
    }
}