using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interstellar.Messages;

public interface IMessageProcessor { 
    int Process(MessageTag tag, ReadOnlySpan<byte> bytes);
}