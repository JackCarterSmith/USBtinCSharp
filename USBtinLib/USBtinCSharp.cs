using System;
using System.IO.Ports;
using System.Threading;

/**
 * Represents an USBtin device.
 * Provide access to an USBtin device (http://www.fischl.de/usbtin) over virtual
 * serial port (CDC).
 *
 * by Thomas Fischl <tfischl@gmx.de>
 * ported by JackCarterSmith <j@bfnt.io>
 */
class USBtinCSharp {
	/** Serial port (virtual) to which USBtin is connected */
	protected SerialPort _serialPort;

	/** Characters coming from USBtin are collected in this StringBuilder */
	//protected StringBuilder incomingMessage = new StringBuilder();

	/** Listener for CAN messages */
	//protected ArrayList<CANMessageListener> listeners = new ArrayList<CANMessageListener>();

	/** Transmit fifo */
	//protected LinkedList<CANMessage> fifoTX = new LinkedList<CANMessage>();

	/** USBtin firmware version */
	protected String firmwareVersion;

	/** USBtin hardware version */
	protected String hardwareVersion;

	/** USBtin serial number */
	protected String serialNumber;

	/** Timeout for response from USBtin */
	protected readonly int TIMEOUT = 1000;

	public enum OpenMode {
		/** Send and receive on CAN bus */
		ACTIVE,
		/** Listen only, sending messages is not possible */
		LISTENONLY,
		/** Loop back the sent CAN messages. Disconnected from physical CAN bus */
		LOOPBACK
	}

	public void Main() {
		Console.WriteLine("Welcome to the C# Station Tutorial!");
	}
}