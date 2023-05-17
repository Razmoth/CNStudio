using System;
using System.IO;
using AssetStudio;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;
using System.Text.RegularExpressions;

namespace AssetStudioCLI
{
    public static class CommandLine
    {
        public static void Init(string[] args)
        {
            var rootCommand = RegisterOptions();
            rootCommand.Invoke(args);
        }
        public static RootCommand RegisterOptions()
        {
            var optionsBinder = new OptionsBinder();
            var rootCommand = new RootCommand()
            {
                optionsBinder.Silent,
                optionsBinder.NameFilter,
                optionsBinder.TypeFilter,
                optionsBinder.ContainerFilter,
                optionsBinder.KeyIndex,
                optionsBinder.MapOp,
                optionsBinder.CABName,
                optionsBinder.MapName,
                optionsBinder.GroupAssetsType,
                optionsBinder.Input,
                optionsBinder.Output
            };

            rootCommand.SetHandler(Program.Run, optionsBinder);

            return rootCommand;
        }
    }

    public class Options
    {
        public bool Silent { get; set; }
        public ClassIDType[] TypeFilter { get; set; }
        public Regex[] NameFilter { get; set; }
        public Regex[] ContainerFilter { get; set; }
        public int KeyIndex { get; set; }
        public MapOpType MapOp { get; set; }
        public string CABName { get; set; }
        public string MapName { get; set; }
        public AssetGroupOption GroupAssetsType { get; set; }
        public FileInfo Input { get; set; }
        public DirectoryInfo Output { get; set; }
    }

    public class OptionsBinder : BinderBase<Options>
    {
        public readonly Option<bool> Silent;
        public readonly Option<ClassIDType[]> TypeFilter;
        public readonly Option<Regex[]> NameFilter;
        public readonly Option<Regex[]> ContainerFilter;
        public readonly Option<int> KeyIndex;
        public readonly Option<MapOpType> MapOp;
        public readonly Option<string> CABName;
        public readonly Option<string> MapName;
        public readonly Option<AssetGroupOption> GroupAssetsType;
        public readonly Argument<FileInfo> Input;
        public readonly Argument<DirectoryInfo> Output;

        public OptionsBinder()
        {
            Silent = new Option<bool>("--silent", "Hide log messages.");
            TypeFilter = new Option<ClassIDType[]>("--types", "Specify unity class type(s)") { AllowMultipleArgumentsPerToken = true, ArgumentHelpName = "Texture2D|Sprite|etc.." };
            NameFilter = new Option<Regex[]>("--names", result => result.Tokens.Select(x => new Regex(x.Value, RegexOptions.IgnoreCase)).ToArray(), false, "Specify name regex filter(s).") { AllowMultipleArgumentsPerToken = true };
            ContainerFilter = new Option<Regex[]>("--containers", result => result.Tokens.Select(x => new Regex(x.Value, RegexOptions.IgnoreCase)).ToArray(), false, "Specify container regex filter(s).") { AllowMultipleArgumentsPerToken = true };
            KeyIndex = new Option<int>("--key_index", $"Specify key index.") { IsRequired = true, ArgumentHelpName = UnityCNKeyManager.ToString() };
            MapOp = new Option<MapOpType>("--map_op", "Specify which map to build.");
            CABName = new Option<string>("--CAB_name", "Specify CABMap file name.");
            MapName = new Option<string>("--map_name", "Specify AssetMap file name.");
            GroupAssetsType = new Option<AssetGroupOption>("--group_assets_type", "Specify how exported assets should be grouped.");
            Input = new Argument<FileInfo>("input_path", "Input file/folder.").LegalFilePathsOnly();
            Output = new Argument<DirectoryInfo>("output_path", "Output folder.").LegalFilePathsOnly();


            TypeFilter.AddValidator(FilterValidator);
            NameFilter.AddValidator(FilterValidator);
            ContainerFilter.AddValidator(FilterValidator);

            CABName.SetDefaultValue("CABMap");
            MapName.SetDefaultValue("assets_map");
            GroupAssetsType.SetDefaultValue(0);
            MapOp.SetDefaultValue(MapOpType.None);
        }

        private void FilterValidator(OptionResult result)
        {
            var values = result.Tokens.Select(x => x.Value).ToArray();
            foreach (var val in values)
            {
                if (string.IsNullOrWhiteSpace(val))
                {
                    result.ErrorMessage = "Empty string.";
                    return;
                }

                try
                {
                    Regex.Match("", val, RegexOptions.IgnoreCase);
                }
                catch (ArgumentException e)
                {
                    result.ErrorMessage = "Invalid Regex.\n" + e.Message;
                    return;
                }
            }
        }

        protected override Options GetBoundValue(BindingContext bindingContext) =>
        new Options
        {
            Silent = bindingContext.ParseResult.GetValueForOption(Silent),
            TypeFilter = bindingContext.ParseResult.GetValueForOption(TypeFilter),
            NameFilter = bindingContext.ParseResult.GetValueForOption(NameFilter),
            ContainerFilter = bindingContext.ParseResult.GetValueForOption(ContainerFilter),
            KeyIndex = bindingContext.ParseResult.GetValueForOption(KeyIndex),
            MapOp = bindingContext.ParseResult.GetValueForOption(MapOp),
            CABName = bindingContext.ParseResult.GetValueForOption(CABName),
            MapName = bindingContext.ParseResult.GetValueForOption(MapName),
            GroupAssetsType = bindingContext.ParseResult.GetValueForOption(GroupAssetsType),
            Input = bindingContext.ParseResult.GetValueForArgument(Input),
            Output = bindingContext.ParseResult.GetValueForArgument(Output)
        };
    }
}
