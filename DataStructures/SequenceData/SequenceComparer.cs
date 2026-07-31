using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DataStructures
{
    public class SequenceComparer
    {
        public class SequenceDifference
        {
            private string description;

            public string Description
            {
                get { return description; }
                set { description = value; }
            }

            public SequenceDifference(string desc)
            {
                this.description = desc;
            }

            public override string ToString()
            {
                return description;
            }
        }

        private delegate bool compareItems<TType>(string path, TType a, TType b, List<SequenceDifference> ans);
        private delegate string itemName<TType>(TType item);

        private class ListEntry<TType>
        {
            public string Key;
            public string DisplayName;
            public int Index;
            public TType Value;
        }

        private class WaveformPair
        {
            public Waveform A;
            public Waveform B;

            public WaveformPair(Waveform a, Waveform b)
            {
                A = a;
                B = b;
            }
        }

        private class ModeEntryInfo
        {
            public string Key;
            public string DisplayName;
            public int Index;
            public SequenceMode.ModeEntry Entry;
        }

        public static List<SequenceDifference> CompareSequences(SequenceData seq1, SequenceData seq2)
        {
            List<SequenceDifference> ans = new List<SequenceDifference>();
            CompareSequencesInternal("Sequence", seq1, seq2, ans, 0);
            return ans;
        }

        private static bool CompareSequencesInternal(string path, SequenceData seq1, SequenceData seq2, List<SequenceDifference> ans, int depth)
        {
            if (seq1 == null || seq2 == null)
                return CompareNulls(path, seq1, seq2, ans);

            if (depth > 3)
            {
                AddDifference(path + " nested sequence comparison skipped because the nesting is too deep.", ans);
                return true;
            }

            bool diffs = false;

            diffs |= CompareStrings(path + ".Name", seq1.SequenceName, seq2.SequenceName, ans);
            diffs |= CompareStrings(path + ".Description", seq1.SequenceDescription, seq2.SequenceDescription, ans);
            diffs |= CompareBools(path + ".WaitForReady", seq1.WaitForReady, seq2.WaitForReady, ans);
            diffs |= CompareBools(path + ".CalibrationShot", seq1.CalibrationShot, seq2.CalibrationShot, ans);
            diffs |= CompareInts(path + ".ListIterationNumber", seq1.ListIterationNumber, seq2.ListIterationNumber, ans);
            diffs |= CompareBools(path + ".StepHidingEnabled", seq1.stepHidingEnabled, seq2.stepHidingEnabled, ans);
            diffs |= CompareBools(path + ".AISaved", seq1.AISaved, seq2.AISaved, ans);
            diffs |= CompareStrings(path + ".CurrentMode", ObjectName(seq1.CurrentMode), ObjectName(seq2.CurrentMode), ans);
            diffs |= CompareCalibrationShots(path + ".CalibrationShots", seq1.CalibrationShotsInfo, seq2.CalibrationShotsInfo, ans, depth);

            diffs |= CompareListsByIdentity<TimeStep>(
                path + ".Timesteps",
                seq1.TimeSteps,
                seq2.TimeSteps,
                ans,
                GetTimestepName,
                CompareTimesteps,
                true);

            diffs |= CompareListsByIdentity<TimestepGroup>(
                path + ".TimestepGroups",
                seq1.TimestepGroups,
                seq2.TimestepGroups,
                ans,
                GetTimestepGroupName,
                CompareTimestepGroups,
                true);

            diffs |= CompareListsByIdentity<AnalogGroup>(
                path + ".AnalogGroups",
                seq1.AnalogGroups,
                seq2.AnalogGroups,
                ans,
                GetAnalogGroupName,
                CompareAnalogGroups,
                true);

            diffs |= CompareListsByIdentity<GPIBGroup>(
                path + ".GpibGroups",
                seq1.GpibGroups,
                seq2.GpibGroups,
                ans,
                GetGpibGroupName,
                CompareGpibGroups,
                true);

            diffs |= CompareListsByIdentity<RS232Group>(
                path + ".RS232Groups",
                seq1.RS232Groups,
                seq2.RS232Groups,
                ans,
                GetRs232GroupName,
                CompareRs232Groups,
                true);

            diffs |= CompareListsByIdentity<Variable>(
                path + ".Variables",
                seq1.Variables,
                seq2.Variables,
                ans,
                GetVariableName,
                CompareVariables,
                true);

            diffs |= CompareListsByIdentity<Pulse>(
                path + ".DigitalPulses",
                seq1.DigitalPulses,
                seq2.DigitalPulses,
                ans,
                GetPulseName,
                ComparePulses,
                true);

            diffs |= CompareListsByIdentity<Waveform>(
                path + ".CommonWaveforms",
                seq1.CommonWaveforms,
                seq2.CommonWaveforms,
                ans,
                GetWaveformName,
                CompareWaveforms,
                true);

            diffs |= CompareListsDatas(path + ".Lists", seq1.Lists, seq2.Lists, ans);

            diffs |= CompareListsByIdentity<SequenceMode>(
                path + ".SequenceModes",
                seq1.SequenceModes,
                seq2.SequenceModes,
                ans,
                GetSequenceModeName,
                delegate(string modePath, SequenceMode a, SequenceMode b, List<SequenceDifference> modeAns)
                {
                    return CompareSequenceModes(modePath, a, b, seq1, seq2, modeAns);
                },
                true);

            return diffs;
        }

        private static bool CompareCalibrationShots(string path, SequenceData.CalibrationShots a, SequenceData.CalibrationShots b, List<SequenceDifference> ans, int depth)
        {
            bool diffs = false;
            diffs |= CompareBools(path + ".Enabled", a.CalibrationShotsEnabled, b.CalibrationShotsEnabled, ans);
            diffs |= CompareBools(path + ".RunFirst", a.RunCalibrationShotFirst, b.RunCalibrationShotFirst, ans);
            diffs |= CompareBools(path + ".RunLast", a.RunCalibrationShotLast, b.RunCalibrationShotLast, ans);
            diffs |= CompareBools(path + ".RunEveryN", a.RunCalibrationShotEveryN, b.RunCalibrationShotEveryN, ans);
            diffs |= CompareInts(path + ".N", a.RunCalibrationShotN, b.RunCalibrationShotN, ans);
            diffs |= CompareSequencesInternal(path + ".Sequence", a.CalibrationShotSequence, b.CalibrationShotSequence, ans, depth + 1);
            return diffs;
        }

        private static bool CompareTimesteps(string path, TimeStep a, TimeStep b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.StepName, b.StepName, ans);
            diffs |= CompareStrings(path + ".Description", a.Description, b.Description, ans);
            diffs |= CompareParameters(path + ".Duration", a.StepDuration, b.StepDuration, ans);
            diffs |= CompareBools(path + ".Enabled", a.StepEnabled, b.StepEnabled, ans);
            diffs |= CompareBools(path + ".Hidden", a.StepHidden, b.StepHidden, ans);
            diffs |= CompareBools(path + ".LoopCopy", a.LoopCopy, b.LoopCopy, ans);
            diffs |= CompareStrings(path + ".TimestepGroup", ObjectName(a.MyTimestepGroup), ObjectName(b.MyTimestepGroup), ans);
            diffs |= CompareStrings(path + ".AnalogGroup", ObjectName(a.AnalogGroup), ObjectName(b.AnalogGroup), ans);
            diffs |= CompareStrings(path + ".GpibGroup", ObjectName(a.GpibGroup), ObjectName(b.GpibGroup), ans);
            diffs |= CompareStrings(path + ".RS232Group", ObjectName(a.rs232Group), ObjectName(b.rs232Group), ans);
            diffs |= CompareStrings(path + ".HotKey", CharSummary(a.HotKeyCharacter), CharSummary(b.HotKeyCharacter), ans);
            diffs |= CompareRetriggerOptions(path + ".Retrigger", a.RetriggerOptions, b.RetriggerOptions, ans);
            diffs |= CompareDictionaries<int, DigitalDataPoint>(path + ".DigitalData", a.DigitalData, b.DigitalData, ans, CompareDigitalDataPoint);
            return diffs;
        }

        private static bool CompareRetriggerOptions(string path, RetriggerOptions a, RetriggerOptions b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareBools(path + ".WaitForRetrigger", a.WaitForRetrigger, b.WaitForRetrigger, ans);
            diffs |= CompareBools(path + ".RetriggerOnEdge", a.RetriggerOnEdge, b.RetriggerOnEdge, ans);
            diffs |= CompareBools(path + ".RetriggerOnNegativeValueOrEdge", a.RetriggerOnNegativeValueOrEdge, b.RetriggerOnNegativeValueOrEdge, ans);
            diffs |= CompareParameters(path + ".Timeout", a.RetriggerTimeout, b.RetriggerTimeout, ans);
            return diffs;
        }

        private static bool CompareTimestepGroups(string path, TimestepGroup a, TimestepGroup b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.TimestepGroupName, b.TimestepGroupName, ans);
            diffs |= CompareBools(path + ".Enabled", a.GroupEnabled, b.GroupEnabled, ans);
            diffs |= CompareBools(path + ".LoopEnabled", a.LoopTimestepGroup, b.LoopTimestepGroup, ans);
            diffs |= CompareParameters(path + ".LoopCount", a.LoopCount, b.LoopCount, ans);
            return diffs;
        }

        private static bool CompareAnalogGroups(string path, AnalogGroup a, AnalogGroup b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.GroupName, b.GroupName, ans);
            diffs |= CompareStrings(path + ".Description", a.GroupDescription, b.GroupDescription, ans);
            diffs |= CompareParameters(path + ".TimeResolution", a.TimeResolution, b.TimeResolution, ans);
            diffs |= CompareDictionaries<int, AnalogGroupChannelData>(path + ".Channels", a.ChannelDatas, b.ChannelDatas, ans, CompareAnalogChannelData);
            return diffs;
        }

        private static bool CompareGpibGroups(string path, GPIBGroup a, GPIBGroup b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.GroupName, b.GroupName, ans);
            diffs |= CompareStrings(path + ".Description", a.GroupDescription, b.GroupDescription, ans);
            diffs |= CompareDictionaries<int, GPIBGroupChannelData>(path + ".Channels", a.ChannelDatas, b.ChannelDatas, ans, CompareGpibChannelData);
            return diffs;
        }

        private static bool CompareRs232Groups(string path, RS232Group a, RS232Group b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.GroupName, b.GroupName, ans);
            diffs |= CompareStrings(path + ".Description", a.GroupDescription, b.GroupDescription, ans);
            diffs |= CompareDictionaries<int, RS232GroupChannelData>(path + ".Channels", a.ChannelDatas, b.ChannelDatas, ans, CompareRs232ChannelData);
            return diffs;
        }

        private static bool CompareAnalogChannelData(string path, AnalogGroupChannelData a, AnalogGroupChannelData b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareBools(path + ".Enabled", a.ChannelEnabled, b.ChannelEnabled, ans);
            diffs |= CompareBools(path + ".UsesCommonWaveform", a.ChannelWaveformIsCommon, b.ChannelWaveformIsCommon, ans);
            diffs |= CompareWaveforms(path + ".Waveform", a.waveform, b.waveform, ans);
            return diffs;
        }

        private static bool CompareGpibChannelData(string path, GPIBGroupChannelData a, GPIBGroupChannelData b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareGenericStructsAsStrings<GPIBGroupChannelData.GpibChannelDataType>(path + ".DataType", a.DataType, b.DataType, ans);
            diffs |= CompareBools(path + ".Enabled", a.Enabled, b.Enabled, ans);
            diffs |= CompareStrings(path + ".RawString", a.RawString, b.RawString, ans);
            diffs |= CompareOrderedLists<StringParameterString>(path + ".StringParameterStrings", a.StringParameterStrings, b.StringParameterStrings, ans, CompareStringParameterStrings);
            diffs |= CompareWaveforms(path + ".FrequencyWaveform", a.frequency, b.frequency, ans);
            diffs |= CompareWaveforms(path + ".VoltageWaveform", a.volts, b.volts, ans);
            return diffs;
        }

        private static bool CompareRs232ChannelData(string path, RS232GroupChannelData a, RS232GroupChannelData b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareGenericStructsAsStrings<RS232GroupChannelData.RS232DataType>(path + ".DataType", a.DataType, b.DataType, ans);
            diffs |= CompareBools(path + ".Enabled", a.Enabled, b.Enabled, ans);
            diffs |= CompareStrings(path + ".RawString", a.RawString, b.RawString, ans);
            diffs |= CompareOrderedLists<StringParameterString>(path + ".StringParameterStrings", a.StringParameterStrings, b.StringParameterStrings, ans, CompareStringParameterStrings);
            return diffs;
        }

        private static bool CompareStringParameterStrings(string path, StringParameterString a, StringParameterString b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Prefix", a.Prefix, b.Prefix, ans);
            diffs |= CompareStrings(path + ".Postfix", a.Postfix, b.Postfix, ans);
            diffs |= CompareParameters(path + ".Parameter", a.Parameter, b.Parameter, ans);
            return diffs;
        }

        private static bool CompareDigitalDataPoint(string path, DigitalDataPoint a, DigitalDataPoint b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Pulse", ObjectName(a.DigitalPulse), ObjectName(b.DigitalPulse), ans);
            diffs |= CompareStrings(path + ".Variable", ObjectName(a.variable), ObjectName(b.variable), ans);
            diffs |= CompareBools(path + ".ManualValue", a.ManualValue, b.ManualValue, ans);
            diffs |= CompareBools(path + ".Continue", a.DigitalContinue, b.DigitalContinue, ans);
            diffs |= CompareBools(path + ".EffectiveValue", a.getValue(), b.getValue(), ans);
            return diffs;
        }

        private static bool CompareWaveforms(string path, Waveform a, Waveform b, List<SequenceDifference> ans)
        {
            return CompareWaveforms(path, a, b, ans, new List<WaveformPair>());
        }

        private static bool CompareWaveforms(string path, Waveform a, Waveform b, List<SequenceDifference> ans, List<WaveformPair> comparedPairs)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            if (AlreadyCompared(a, b, comparedPairs))
                return false;

            comparedPairs.Add(new WaveformPair(a, b));

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.WaveformName, b.WaveformName, ans);
            diffs |= CompareParameters(path + ".Duration", a.WaveformDuration, b.WaveformDuration, ans);
            diffs |= CompareGenericStructsAsStrings<Units.Dimension>(path + ".YUnits", a.YUnits, b.YUnits, ans);
            diffs |= CompareGenericStructsAsStrings<Waveform.InterpolationType>(path + ".Interpolation", a.interpolationType, b.interpolationType, ans);
            diffs |= CompareStrings(path + ".DataFileName", a.DataFileName, b.DataFileName, ans);
            diffs |= CompareBools(path + ".DataFromFile", a.DataFromFile, b.DataFromFile, ans);
            diffs |= CompareStrings(path + ".EquationString", a.EquationString, b.EquationString, ans);
            diffs |= CompareOrderedLists<DimensionedParameter>(path + ".XValues", a.XValues, b.XValues, ans, CompareParameters);
            diffs |= CompareOrderedLists<DimensionedParameter>(path + ".YValues", a.YValues, b.YValues, ans, CompareParameters);
            diffs |= CompareOrderedLists<DimensionedParameter>(path + ".ExtraParameters", a.ExtraParameters, b.ExtraParameters, ans, CompareParameters);
            diffs |= CompareOrderedLists<Waveform.InterpolationType.CombinationOperators>(
                path + ".Combiners",
                a.WaveformCombiners,
                b.WaveformCombiners,
                ans,
                CompareGenericStructsAsStrings<Waveform.InterpolationType.CombinationOperators>);
            diffs |= CompareWaveformReferenceLists(path + ".ReferencedWaveforms", a.ReferencedWaveforms, b.ReferencedWaveforms, ans, comparedPairs);
            return diffs;
        }

        private static bool CompareWaveformReferenceLists(string path, List<Waveform> a, List<Waveform> b, List<SequenceDifference> ans, List<WaveformPair> comparedPairs)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            if (a.Count != b.Count)
            {
                AddChanged(path + ".Count", a.Count, b.Count, ans);
                diffs = true;
            }

            int common = Math.Min(a.Count, b.Count);
            for (int i = 0; i < common; i++)
            {
                string itemPath = path + "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]";
                diffs |= CompareStrings(itemPath + ".ReferenceName", ObjectName(a[i]), ObjectName(b[i]), ans);
                diffs |= CompareWaveforms(itemPath, a[i], b[i], ans, comparedPairs);
            }

            for (int i = common; i < a.Count; i++)
            {
                AddRemoved(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], ans);
                diffs = true;
            }

            for (int i = common; i < b.Count; i++)
            {
                AddAdded(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", b[i], ans);
                diffs = true;
            }

            return diffs;
        }

        private static bool CompareVariables(string path, Variable a, Variable b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.VariableName, b.VariableName, ans);
            diffs |= CompareStrings(path + ".Description", a.Description, b.Description, ans);
            diffs |= CompareBools(path + ".Relevant", a.Relevant, b.Relevant, ans);
            diffs |= CompareBools(path + ".Derived", a.DerivedVariable, b.DerivedVariable, ans);
            diffs |= CompareBools(path + ".Special", a.IsSpecialVariable, b.IsSpecialVariable, ans);
            diffs |= CompareGenericStructsAsStrings<Variable.SpecialVariableType>(path + ".SpecialType", a.MySpecialVariableType, b.MySpecialVariableType, ans);
            diffs |= CompareBools(path + ".ListDriven", a.ListDriven, b.ListDriven, ans);
            diffs |= CompareInts(path + ".ListNumber", a.ListNumber, b.ListNumber, ans);
            diffs |= CompareBools(path + ".Permanent", a.PermanentVariable, b.PermanentVariable, ans);
            diffs |= CompareDoubles(path + ".PermanentValue", a.PermanentValue, b.PermanentValue, ans);
            diffs |= CompareStrings(path + ".Formula", a.VariableFormula, b.VariableFormula, ans);
            diffs |= CompareDoubles(path + ".Value", a.VariableValue, b.VariableValue, ans);
            return diffs;
        }

        private static bool ComparePulses(string path, Pulse a, Pulse b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.PulseName, b.PulseName, ans);
            diffs |= CompareStrings(path + ".Description", a.PulseDescription, b.PulseDescription, ans);
            diffs |= CompareBools(path + ".AutoName", a.AutoName, b.AutoName, ans);
            diffs |= CompareBools(path + ".Value", a.PulseValue, b.PulseValue, ans);
            diffs |= CompareBools(path + ".ValueFromVariable", a.ValueFromVariable, b.ValueFromVariable, ans);
            diffs |= CompareStrings(path + ".ValueVariable", ObjectName(a.ValueVariable), ObjectName(b.ValueVariable), ans);
            diffs |= CompareGenericStructsAsStrings<Pulse.PulseTimingCondition>(path + ".StartCondition", a.startCondition, b.startCondition, ans);
            diffs |= CompareParameters(path + ".StartDelay", a.startDelay, b.startDelay, ans);
            diffs |= CompareBools(path + ".StartDelayed", a.startDelayed, b.startDelayed, ans);
            diffs |= CompareBools(path + ".StartDelayEnabled", a.startDelayEnabled, b.startDelayEnabled, ans);
            diffs |= CompareGenericStructsAsStrings<Pulse.PulseTimingCondition>(path + ".EndCondition", a.endCondition, b.endCondition, ans);
            diffs |= CompareParameters(path + ".EndDelay", a.endDelay, b.endDelay, ans);
            diffs |= CompareBools(path + ".EndDelayed", a.endDelayed, b.endDelayed, ans);
            diffs |= CompareBools(path + ".EndDelayEnabled", a.endDelayEnabled, b.endDelayEnabled, ans);
            diffs |= CompareParameters(path + ".Duration", a.pulseDuration, b.pulseDuration, ans);
            return diffs;
        }

        private static bool CompareSequenceModes(string path, SequenceMode a, SequenceMode b, SequenceData seqA, SequenceData seqB, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Name", a.ModeName, b.ModeName, ans);
            diffs |= CompareStrings(path + ".Description", a.ModeDescription, b.ModeDescription, ans);
            diffs |= CompareModeEntries(path + ".Entries", a, b, seqA, seqB, ans);
            return diffs;
        }

        private static bool CompareModeEntries(string path, SequenceMode a, SequenceMode b, SequenceData seqA, SequenceData seqB, List<SequenceDifference> ans)
        {
            List<ModeEntryInfo> entriesA = BuildModeEntryInfos(a, seqA, path + " base", ans);
            List<ModeEntryInfo> entriesB = BuildModeEntryInfos(b, seqB, path + " compared", ans);
            Dictionary<string, ModeEntryInfo> dictA = BuildModeEntryDictionary(entriesA);
            Dictionary<string, ModeEntryInfo> dictB = BuildModeEntryDictionary(entriesB);

            bool diffs = false;

            foreach (ModeEntryInfo entry in entriesA)
            {
                if (!dictB.ContainsKey(entry.Key))
                {
                    AddRemoved(path, entry.DisplayName, entry.Entry, ans);
                    diffs = true;
                }
            }

            foreach (ModeEntryInfo entry in entriesB)
            {
                if (!dictA.ContainsKey(entry.Key))
                {
                    AddAdded(path, entry.DisplayName, entry.Entry, ans);
                    diffs = true;
                }
            }

            foreach (ModeEntryInfo entryA in entriesA)
            {
                if (dictB.ContainsKey(entryA.Key))
                {
                    ModeEntryInfo entryB = dictB[entryA.Key];
                    if (entryA.Index != entryB.Index)
                    {
                        AddChanged(path + "[" + entryA.DisplayName + "].Index", entryA.Index + 1, entryB.Index + 1, ans);
                        diffs = true;
                    }
                    diffs |= CompareSequenceModeEntry(path + "[" + entryA.DisplayName + "]", entryA.Entry, entryB.Entry, ans);
                }
            }

            return diffs;
        }

        private static List<ModeEntryInfo> BuildModeEntryInfos(SequenceMode mode, SequenceData sequence, string path, List<SequenceDifference> ans)
        {
            List<ModeEntryInfo> infos = new List<ModeEntryInfo>();
            if (mode == null || mode.TimestepEntries == null || sequence == null || sequence.TimeSteps == null)
                return infos;

            List<ListEntry<TimeStep>> stepEntries = BuildListEntries<TimeStep>(sequence.TimeSteps, GetTimestepName);
            Dictionary<TimeStep, string> knownSteps = new Dictionary<TimeStep, string>();

            foreach (ListEntry<TimeStep> stepEntry in stepEntries)
            {
                if (!knownSteps.ContainsKey(stepEntry.Value))
                    knownSteps.Add(stepEntry.Value, stepEntry.Key);
                if (mode.TimestepEntries.ContainsKey(stepEntry.Value))
                {
                    ModeEntryInfo info = new ModeEntryInfo();
                    info.Key = stepEntry.Key;
                    info.DisplayName = stepEntry.DisplayName;
                    info.Index = stepEntry.Index;
                    info.Entry = mode.TimestepEntries[stepEntry.Value];
                    infos.Add(info);
                }
                else
                {
                    AddDifference(path + " missing entry for timestep " + stepEntry.DisplayName + ".", ans);
                }
            }

            foreach (TimeStep step in mode.TimestepEntries.Keys)
            {
                if (!knownSteps.ContainsKey(step))
                {
                    AddDifference(path + " contains stale entry for timestep " + ObjectSummary(step) + ".", ans);
                }
            }

            return infos;
        }

        private static Dictionary<string, ModeEntryInfo> BuildModeEntryDictionary(List<ModeEntryInfo> entries)
        {
            Dictionary<string, ModeEntryInfo> ans = new Dictionary<string, ModeEntryInfo>();
            foreach (ModeEntryInfo entry in entries)
            {
                if (!ans.ContainsKey(entry.Key))
                    ans.Add(entry.Key, entry);
            }
            return ans;
        }

        private static bool CompareSequenceModeEntry(string path, SequenceMode.ModeEntry a, SequenceMode.ModeEntry b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareBools(path + ".Enabled", a.StepEnabled, b.StepEnabled, ans);
            diffs |= CompareBools(path + ".Hidden", a.StepHidden, b.StepHidden, ans);
            return diffs;
        }

        private static bool CompareListsDatas(string path, ListData a, ListData b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareBools(path + ".Locked", a.ListLocked, b.ListLocked, ans);
            diffs |= CompareBoolArray(path + ".Cross", a.Cross, b.Cross, ans);
            diffs |= CompareBoolArray(path + ".Enabled", a.ListEnabled, b.ListEnabled, ans);
            diffs |= CompareListDataValues(path + ".Values", a.Lists, b.Lists, ans);
            return diffs;
        }

        private static bool CompareListDataValues(string path, List<double>[] a, List<double>[] b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            if (a.Length != b.Length)
            {
                AddChanged(path + ".Count", a.Length, b.Length, ans);
                diffs = true;
            }

            int common = Math.Min(a.Length, b.Length);
            for (int i = 0; i < common; i++)
            {
                diffs |= CompareDoubleLists(path + "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], b[i], ans);
            }

            for (int i = common; i < a.Length; i++)
            {
                AddRemoved(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], ans);
                diffs = true;
            }

            for (int i = common; i < b.Length; i++)
            {
                AddAdded(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", b[i], ans);
                diffs = true;
            }

            return diffs;
        }

        private static bool CompareDoubleLists(string path, List<double> a, List<double> b, List<SequenceDifference> ans)
        {
            return CompareOrderedLists<double>(path, a, b, ans, CompareDoubles);
        }

        private static bool CompareBoolArray(string path, bool[] a, bool[] b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            if (a.Length != b.Length)
            {
                AddChanged(path + ".Count", a.Length, b.Length, ans);
                diffs = true;
            }

            int common = Math.Min(a.Length, b.Length);
            for (int i = 0; i < common; i++)
            {
                diffs |= CompareBools(path + "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], b[i], ans);
            }

            for (int i = common; i < a.Length; i++)
            {
                AddRemoved(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], ans);
                diffs = true;
            }

            for (int i = common; i < b.Length; i++)
            {
                AddAdded(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", b[i], ans);
                diffs = true;
            }

            return diffs;
        }

        private static bool CompareOrderedLists<TVal>(string path, List<TVal> a, List<TVal> b, List<SequenceDifference> ans, compareItems<TVal> compareValues)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            if (a.Count != b.Count)
            {
                AddChanged(path + ".Count", a.Count, b.Count, ans);
                diffs = true;
            }

            int common = Math.Min(a.Count, b.Count);
            for (int i = 0; i < common; i++)
            {
                diffs |= compareValues(path + "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], b[i], ans);
            }

            for (int i = common; i < a.Count; i++)
            {
                AddRemoved(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", a[i], ans);
                diffs = true;
            }

            for (int i = common; i < b.Count; i++)
            {
                AddAdded(path, "[" + (i + 1).ToString(CultureInfo.InvariantCulture) + "]", b[i], ans);
                diffs = true;
            }

            return diffs;
        }

        private static bool CompareListsByIdentity<TVal>(string path, List<TVal> a, List<TVal> b, List<SequenceDifference> ans, itemName<TVal> getName, compareItems<TVal> compareValues, bool orderMatters)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            List<ListEntry<TVal>> entriesA = BuildListEntries<TVal>(a, getName);
            List<ListEntry<TVal>> entriesB = BuildListEntries<TVal>(b, getName);
            Dictionary<string, ListEntry<TVal>> dictA = BuildEntryDictionary<TVal>(entriesA);
            Dictionary<string, ListEntry<TVal>> dictB = BuildEntryDictionary<TVal>(entriesB);

            bool diffs = false;
            if (a.Count != b.Count)
            {
                AddChanged(path + ".Count", a.Count, b.Count, ans);
                diffs = true;
            }

            foreach (ListEntry<TVal> entry in entriesA)
            {
                if (!dictB.ContainsKey(entry.Key))
                {
                    AddRemoved(path, entry.DisplayName, entry.Value, ans);
                    diffs = true;
                }
            }

            foreach (ListEntry<TVal> entry in entriesB)
            {
                if (!dictA.ContainsKey(entry.Key))
                {
                    AddAdded(path, entry.DisplayName, entry.Value, ans);
                    diffs = true;
                }
            }

            foreach (ListEntry<TVal> entryA in entriesA)
            {
                if (dictB.ContainsKey(entryA.Key))
                {
                    ListEntry<TVal> entryB = dictB[entryA.Key];
                    if (orderMatters && entryA.Index != entryB.Index)
                    {
                        AddChanged(path + "[" + entryA.DisplayName + "].Index", entryA.Index + 1, entryB.Index + 1, ans);
                        diffs = true;
                    }

                    diffs |= compareValues(path + "[" + entryA.DisplayName + "]", entryA.Value, entryB.Value, ans);
                }
            }

            Dictionary<int, ListEntry<TVal>> unmatchedByIndexA = new Dictionary<int, ListEntry<TVal>>();
            Dictionary<int, ListEntry<TVal>> unmatchedByIndexB = new Dictionary<int, ListEntry<TVal>>();

            foreach (ListEntry<TVal> entry in entriesA)
            {
                if (!dictB.ContainsKey(entry.Key) && !unmatchedByIndexA.ContainsKey(entry.Index))
                    unmatchedByIndexA.Add(entry.Index, entry);
            }

            foreach (ListEntry<TVal> entry in entriesB)
            {
                if (!dictA.ContainsKey(entry.Key) && !unmatchedByIndexB.ContainsKey(entry.Index))
                    unmatchedByIndexB.Add(entry.Index, entry);
            }

            foreach (int index in unmatchedByIndexA.Keys)
            {
                if (unmatchedByIndexB.ContainsKey(index))
                {
                    ListEntry<TVal> entryA = unmatchedByIndexA[index];
                    ListEntry<TVal> entryB = unmatchedByIndexB[index];
                    string indexPath = path + "[Index " + (index + 1).ToString(CultureInfo.InvariantCulture) + "]";
                    AddDifference(indexPath + ".Identity changed: " + entryA.DisplayName + " -> " + entryB.DisplayName + ".", ans);
                    diffs = true;
                    diffs |= compareValues(indexPath, entryA.Value, entryB.Value, ans);
                }
            }

            return diffs;
        }

        private static List<ListEntry<TVal>> BuildListEntries<TVal>(List<TVal> list, itemName<TVal> getName)
        {
            List<ListEntry<TVal>> entries = new List<ListEntry<TVal>>();
            Dictionary<string, int> occurrences = new Dictionary<string, int>();

            for (int i = 0; i < list.Count; i++)
            {
                string name = getName(list[i]);
                string baseKey = String.IsNullOrEmpty(name) ? "<unnamed>" : name;
                int occurrence = 1;
                if (occurrences.ContainsKey(baseKey))
                {
                    occurrence = occurrences[baseKey] + 1;
                    occurrences[baseKey] = occurrence;
                }
                else
                {
                    occurrences.Add(baseKey, occurrence);
                }

                ListEntry<TVal> entry = new ListEntry<TVal>();
                entry.Key = baseKey + "\u001f" + occurrence.ToString(CultureInfo.InvariantCulture);
                entry.DisplayName = MakeListDisplayName(name, i, occurrence);
                entry.Index = i;
                entry.Value = list[i];
                entries.Add(entry);
            }

            return entries;
        }

        private static Dictionary<string, ListEntry<TVal>> BuildEntryDictionary<TVal>(List<ListEntry<TVal>> entries)
        {
            Dictionary<string, ListEntry<TVal>> ans = new Dictionary<string, ListEntry<TVal>>();
            foreach (ListEntry<TVal> entry in entries)
            {
                if (!ans.ContainsKey(entry.Key))
                    ans.Add(entry.Key, entry);
            }
            return ans;
        }

        private static bool CompareDictionaries<TKey, TVal>(string path, Dictionary<TKey, TVal> d1, Dictionary<TKey, TVal> d2, List<SequenceDifference> ans, compareItems<TVal> compareValues)
        {
            if (d1 == null || d2 == null)
                return CompareNulls(path, d1, d2, ans);

            bool diffs = false;
            List<TKey> keys1 = SortedKeys<TKey, TVal>(d1);
            List<TKey> keys2 = SortedKeys<TKey, TVal>(d2);

            foreach (TKey key in keys1)
            {
                if (!d2.ContainsKey(key))
                {
                    AddRemoved(path, "[" + KeySummary(key) + "]", d1[key], ans);
                    diffs = true;
                }
            }

            foreach (TKey key in keys2)
            {
                if (!d1.ContainsKey(key))
                {
                    AddAdded(path, "[" + KeySummary(key) + "]", d2[key], ans);
                    diffs = true;
                }
            }

            foreach (TKey key in keys1)
            {
                if (d2.ContainsKey(key))
                {
                    diffs |= compareValues(path + "[" + KeySummary(key) + "]", d1[key], d2[key], ans);
                }
            }

            return diffs;
        }

        private static List<TKey> SortedKeys<TKey, TVal>(Dictionary<TKey, TVal> dictionary)
        {
            List<TKey> keys = new List<TKey>();
            foreach (TKey key in dictionary.Keys)
                keys.Add(key);

            keys.Sort(delegate(TKey a, TKey b)
            {
                return String.Compare(KeySummary(a), KeySummary(b), StringComparison.Ordinal);
            });

            return keys;
        }

        private static bool CompareParameters(string path, DimensionedParameter a, DimensionedParameter b, List<SequenceDifference> ans)
        {
            if (a == null || b == null)
                return CompareNulls(path, a, b, ans);

            bool diffs = false;
            diffs |= CompareStrings(path + ".Binding", ParameterBinding(a), ParameterBinding(b), ans);
            diffs |= CompareStrings(path + ".Units", UnitSummary(a.ParameterUnits), UnitSummary(b.ParameterUnits), ans);

            if (a.myParameter.variable == null && b.myParameter.variable == null)
            {
                diffs |= CompareDoubles(path + ".ManualBaseValue", a.getBaseValue(), b.getBaseValue(), ans);
            }

            return diffs;
        }

        private static bool CompareGenericStructsAsStrings<TType>(string path, TType a, TType b, List<SequenceDifference> ans) where TType : struct
        {
            return CompareStrings(path, a.ToString(), b.ToString(), ans);
        }

        private static bool CompareInts(string path, int a, int b, List<SequenceDifference> ans)
        {
            if (a != b)
            {
                AddChanged(path, a, b, ans);
                return true;
            }
            return false;
        }

        private static bool CompareDoubles(string path, double a, double b, List<SequenceDifference> ans)
        {
            if (a != b)
            {
                AddChanged(path, a, b, ans);
                return true;
            }
            return false;
        }

        private static bool CompareBools(string path, bool a, bool b, List<SequenceDifference> ans)
        {
            if (a != b)
            {
                AddChanged(path, a, b, ans);
                return true;
            }
            return false;
        }

        private static bool CompareStrings(string path, string a, string b, List<SequenceDifference> ans)
        {
            if (a != b)
            {
                AddChanged(path, a, b, ans);
                return true;
            }
            return false;
        }

        private static bool CompareNulls(string path, object a, object b, List<SequenceDifference> ans)
        {
            if (a == null && b == null)
                return false;

            AddChanged(path, ObjectSummary(a), ObjectSummary(b), ans);
            return true;
        }

        private static bool AlreadyCompared(Waveform a, Waveform b, List<WaveformPair> comparedPairs)
        {
            foreach (WaveformPair pair in comparedPairs)
            {
                if (Object.ReferenceEquals(pair.A, a) && Object.ReferenceEquals(pair.B, b))
                    return true;
            }
            return false;
        }

        private static void AddDifference(string difference, List<SequenceDifference> ans)
        {
            ans.Add(new SequenceDifference(difference));
        }

        private static void AddChanged(string path, object a, object b, List<SequenceDifference> ans)
        {
            AddDifference(path + " changed: " + ObjectSummary(a) + " -> " + ObjectSummary(b) + ".", ans);
        }

        private static void AddAdded(string path, string item, object value, List<SequenceDifference> ans)
        {
            AddDifference(path + " added " + item + ": " + ObjectSummary(value) + ".", ans);
        }

        private static void AddRemoved(string path, string item, object value, List<SequenceDifference> ans)
        {
            AddDifference(path + " removed " + item + ": " + ObjectSummary(value) + ".", ans);
        }

        private static string ObjectSummary(object value)
        {
            if (value == null)
                return "<null>";

            if (value is string)
                return StringSummary((string)value);

            if (value is bool)
                return BoolString((bool)value);

            if (value is double)
                return ((double)value).ToString("R", CultureInfo.InvariantCulture);

            if (value is float)
                return ((float)value).ToString("R", CultureInfo.InvariantCulture);

            if (value is decimal)
                return ((decimal)value).ToString(CultureInfo.InvariantCulture);

            if (value is int || value is long || value is short || value is byte)
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            List<double> doubleList = value as List<double>;
            if (doubleList != null)
                return "List<double> count " + doubleList.Count.ToString(CultureInfo.InvariantCulture);

            SequenceMode.ModeEntry modeEntry = value as SequenceMode.ModeEntry;
            if (modeEntry != null)
                return "Enabled=" + BoolString(modeEntry.StepEnabled) + ", Hidden=" + BoolString(modeEntry.StepHidden);

            TimeStep timeStep = value as TimeStep;
            if (timeStep != null)
                return "TimeStep " + StringSummary(timeStep.StepName);

            TimestepGroup timestepGroup = value as TimestepGroup;
            if (timestepGroup != null)
                return "TimestepGroup " + StringSummary(timestepGroup.TimestepGroupName);

            AnalogGroup analogGroup = value as AnalogGroup;
            if (analogGroup != null)
                return "AnalogGroup " + StringSummary(analogGroup.GroupName);

            GPIBGroup gpibGroup = value as GPIBGroup;
            if (gpibGroup != null)
                return "GPIBGroup " + StringSummary(gpibGroup.GroupName);

            RS232Group rs232Group = value as RS232Group;
            if (rs232Group != null)
                return "RS232Group " + StringSummary(rs232Group.GroupName);

            Variable variable = value as Variable;
            if (variable != null)
                return "Variable " + StringSummary(variable.VariableName);

            Pulse pulse = value as Pulse;
            if (pulse != null)
                return "Pulse " + StringSummary(pulse.PulseName);

            Waveform waveform = value as Waveform;
            if (waveform != null)
                return "Waveform " + StringSummary(waveform.WaveformName);

            SequenceMode mode = value as SequenceMode;
            if (mode != null)
                return "SequenceMode " + StringSummary(mode.ModeName);

            return StringSummary(value.ToString());
        }

        private static string StringSummary(string value)
        {
            if (value == null)
                return "<null>";

            return "\"" + value.Replace("\r", "\\r").Replace("\n", "\\n") + "\"";
        }

        private static string BoolString(bool value)
        {
            return value ? "true" : "false";
        }

        private static string KeySummary(object key)
        {
            if (key == null)
                return "<null>";
            return key.ToString();
        }

        private static string CharSummary(char value)
        {
            if (value == 0)
                return "<none>";
            return value.ToString();
        }

        private static string UnitSummary(Units units)
        {
            return units.ToString();
        }

        private static string ParameterBinding(DimensionedParameter parameter)
        {
            if (parameter == null)
                return null;

            if (parameter.myParameter.variable == null)
                return "manual";

            return "variable " + parameter.myParameter.variable.VariableName;
        }

        private static string ObjectName(object obj)
        {
            if (obj == null)
                return null;

            return obj.ToString();
        }

        private static string MakeListDisplayName(string name, int index, int occurrence)
        {
            if (String.IsNullOrEmpty(name))
                return "#" + (index + 1).ToString(CultureInfo.InvariantCulture) + " <unnamed> #" + occurrence.ToString(CultureInfo.InvariantCulture);

            return StringSummary(name) + " #" + occurrence.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetTimestepName(TimeStep item)
        {
            if (item == null)
                return null;
            return item.StepName;
        }

        private static string GetTimestepGroupName(TimestepGroup item)
        {
            if (item == null)
                return null;
            return item.TimestepGroupName;
        }

        private static string GetAnalogGroupName(AnalogGroup item)
        {
            if (item == null)
                return null;
            return item.GroupName;
        }

        private static string GetGpibGroupName(GPIBGroup item)
        {
            if (item == null)
                return null;
            return item.GroupName;
        }

        private static string GetRs232GroupName(RS232Group item)
        {
            if (item == null)
                return null;
            return item.GroupName;
        }

        private static string GetVariableName(Variable item)
        {
            if (item == null)
                return null;
            return item.VariableName;
        }

        private static string GetPulseName(Pulse item)
        {
            if (item == null)
                return null;
            return item.PulseName;
        }

        private static string GetWaveformName(Waveform item)
        {
            if (item == null)
                return null;
            return item.WaveformName;
        }

        private static string GetSequenceModeName(SequenceMode item)
        {
            if (item == null)
                return null;
            return item.ModeName;
        }
    }
}
