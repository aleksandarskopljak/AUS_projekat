using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)CommandParameters.Length)), 0, request, 4, 2);
            request[6] = CommandParameters.UnitId;
            request[7] = CommandParameters.FunctionCode;

            ushort address = ((ModbusWriteCommandParameters)CommandParameters).OutputAddress;
            ushort value = ((ModbusWriteCommandParameters)CommandParameters).Value;

            ushort coilValue = value == 1 ? (ushort)0xFF00 : (ushort)0x0000;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)address)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)coilValue)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            var result = new Dictionary<Tuple<PointType, ushort>, ushort>();

            if (response[7] == CommandParameters.FunctionCode + 0x80)
            {
                HandeException(response[8]);
                return result;
            }

            ushort address = BitConverter.ToUInt16(response, 8);
            ushort rawValue = BitConverter.ToUInt16(response, 10);

            address = (ushort)IPAddress.NetworkToHostOrder((short)address);
            rawValue = (ushort)IPAddress.NetworkToHostOrder((short)rawValue);

            ushort value = rawValue == 0xFF00 ? (ushort)1 : (ushort)0;

            result.Add(
                new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address),
                value);

            return result;
        }
    }
}