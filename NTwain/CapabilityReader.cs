﻿using NTwain.Data;
using NTwain.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace NTwain
{
    /// <summary>
    /// The one-stop class for reading raw TWAIN cap values.
    /// This contains all the properties for the 4 container types.
    /// </summary>
    public class CapabilityReader
    {
        /// <summary>
        /// Reads the value from a <see cref="TWCapability" /> that was returned
        /// from a TWAIN source.
        /// </summary>
        /// <param name="capability">The capability.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">capability</exception>
        /// <exception cref="System.ArgumentException">Capability contains no data.;capability
        /// or
        /// capability</exception>
        public static CapabilityReader ReadValue(TWCapability capability)
        {
            return ReadValue(capability, PlatformInfo.Current);
        }

        /// <summary>
        /// Reads the value from a <see cref="TWCapability" /> that was returned
        /// from a TWAIN source.
        /// </summary>
        /// <param name="capability">The capability.</param>
        /// <param name="platformInfo">The platform information.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">
        /// capability
        /// or
        /// platformInfo
        /// </exception>
        /// <exception cref="System.ArgumentException">Capability contains no data.;capability
        /// or
        /// capability</exception>
        public static CapabilityReader ReadValue(TWCapability capability, IPlatformInfo platformInfo)
        {
            if (capability == null) { throw new ArgumentNullException("capability"); }
            if (capability.Container == IntPtr.Zero) { throw new ArgumentException(Resources.CapHasNoData, "capability"); }
            if (platformInfo == null) { throw new ArgumentNullException("platformInfo"); }

            IntPtr baseAddr = IntPtr.Zero;
            try
            {
                baseAddr = platformInfo.MemoryManager.Lock(capability.Container);
                switch (capability.ContainerType)
                {
                    case ContainerType.Array:
                        return new CapabilityReader
                        {
                            ContainerType = capability.ContainerType,
                        }.ReadArrayValue(baseAddr);
                    case ContainerType.Enum:
                        return new CapabilityReader
                        {
                            ContainerType = capability.ContainerType,
                        }.ReadEnumValue(baseAddr);
                    case ContainerType.OneValue:
                        return new CapabilityReader
                        {
                            ContainerType = capability.ContainerType,
                        }.ReadOneValue(baseAddr);
                    case ContainerType.Range:
                        return new CapabilityReader
                        {
                            ContainerType = capability.ContainerType,
                        }.ReadRangeValue(baseAddr);
                    default:
                        throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.CapHasBadContainer, capability.ContainerType), "capability");
                }
            }
            finally
            {
                if (baseAddr != IntPtr.Zero)
                {
                    platformInfo.MemoryManager.Unlock(baseAddr);
                }
            }
        }

        #region common prop

        /// <summary>
        /// Gets the underlying container type. 
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        public ContainerType ContainerType { get; private set; }

        /// <summary>
        /// Gets the type of the TWAIN value.
        /// </summary>
        /// <value>
        /// The type of the value.
        /// </value>
        public ItemType ItemType { get; private set; }

        /// <summary>
        /// Gets the one value if container is <see cref="NTwain.Data.ContainerType.Array"/>.
        /// </summary>
        /// <value>
        /// The one value.
        /// </value>
        public object OneValue { get; private set; }

        /// <summary>
        /// Gets the collection values if container is <see cref="NTwain.Data.ContainerType.Enum"/> or <see cref="NTwain.Data.ContainerType.Range"/> .
        /// </summary>
        /// <value>
        /// The collection values.
        /// </value>
        public IList<object> CollectionValues { get; private set; }

        #endregion

        #region enum prop

        /// <summary>
        /// Gets the current value index if container is <see cref="NTwain.Data.ContainerType.Enum"/>.
        /// </summary>
        public int EnumCurrentIndex { get; private set; }
        /// <summary>
        /// Gets the default value index if container is <see cref="NTwain.Data.ContainerType.Enum" />.
        /// </summary>
        public int EnumDefaultIndex { get; private set; }

        #endregion

        #region range prop

        /// <summary>
        /// Gets the current value if container is <see cref="NTwain.Data.ContainerType.Range" />.
        /// </summary>
        /// <value>
        /// The range current value.
        /// </value>
        public object RangeCurrentValue { get; private set; }
        /// <summary>
        /// Gets the default value if container is <see cref="NTwain.Data.ContainerType.Range" />.
        /// </summary>
        /// <value>
        /// The range default value.
        /// </value>
        public object RangeDefaultValue { get; private set; }
        /// <summary>
        /// The least positive/most negative value of the range.
        /// </summary>
        /// <value>
        /// The range minimum value.
        /// </value>
        public uint RangeMinValue { get; private set; }
        /// <summary>
        /// The most positive/least negative value of the range.
        /// </summary>
        /// <value>
        /// The range maximum value.
        /// </value>
        public uint RangeMaxValue { get; private set; }
        /// <summary>
        /// The delta between two adjacent values of the range.
        /// e.g. Item2 - Item1 = StepSize;
        /// </summary>
        /// <value>
        /// The size of the range step.
        /// </value>
        public uint RangeStepSize { get; private set; }

        #endregion

        #region reader methods

        CapabilityReader ReadOneValue(IntPtr baseAddr)
        {
            int offset = 0;
            ItemType = (ItemType)(ushort)Marshal.ReadInt16(baseAddr, offset);
            offset += 2;
            OneValue = TypeReader.ReadValue(baseAddr, ref offset, ItemType);
            return this;
        }

        CapabilityReader ReadArrayValue(IntPtr baseAddr)
        {
            int offset = 0;
            ItemType = (ItemType)(ushort)Marshal.ReadInt16(baseAddr, offset);
            offset += 2;
            var count = Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            if (count > 0)
            {
                CollectionValues = new object[count];
                for (int i = 0; i < count; i++)
                {
                    CollectionValues[i] = TypeReader.ReadValue(baseAddr, ref offset, ItemType);
                }
            }
            return this;
        }

        CapabilityReader ReadEnumValue(IntPtr baseAddr)
        {
            int offset = 0;
            ItemType = (ItemType)(ushort)Marshal.ReadInt16(baseAddr, offset);
            offset += 2;
            int count = Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            EnumCurrentIndex = Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            EnumDefaultIndex = Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            if (count > 0)
            {
                CollectionValues = new object[count];
                for (int i = 0; i < count; i++)
                {
                    CollectionValues[i] = TypeReader.ReadValue(baseAddr, ref offset, ItemType);
                }
            }
            return this;
        }

        CapabilityReader ReadRangeValue(IntPtr baseAddr)
        {
            int offset = 0;
            ItemType = (ItemType)(ushort)Marshal.ReadInt16(baseAddr, offset);
            offset += 2;
            RangeMinValue = (uint)Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            RangeMaxValue = (uint)Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            RangeStepSize = (uint)Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            RangeDefaultValue = (uint)Marshal.ReadInt32(baseAddr, offset);
            offset += 4;
            RangeCurrentValue = (uint)Marshal.ReadInt32(baseAddr, offset);

            return this;
        }

        #endregion
    }
}
