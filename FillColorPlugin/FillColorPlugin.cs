using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FillColorPlugin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FillColorPlugin : IExternalCommand
    {
        #region Const

        private static readonly string ROM_ZoneName = "ROM_Зона";
        // private static readonly string LevelNameRu = "Уровень";
        private static readonly string LevelNameEng = "Level";
        private static readonly string BS_BlockName = "BS_Блок";
        private static readonly string ROM_SubZoneName = "ROM_Подзона";
        // private static readonly string RoomExpr = @"Квартира (\d{2})";
        private static readonly string Room = "Квартира";
        private static readonly string ROM_Calc_SubZoneIdName = "ROM_Расчетная_подзона_ID";
        private static readonly string ROM_SubZoneIndexName = "ROM_Подзона_Index";
        private static readonly string ColorSuffix = ".Полутон";
        private static readonly string TransactionName = "FillColorTransaction";
        private static readonly string ErrorBoxTitle = "Error";

        #endregion

        #region IExternalCommand

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            FilteredElementCollector newRoomFilter = new FilteredElementCollector(doc);
            ICollection<Element> allRooms = newRoomFilter.OfCategory(BuiltInCategory.OST_Rooms).WhereElementIsNotElementType().ToElements();

            var roomGroupsByLevel = allRooms.Where(element => GetParameterValue(element.Parameters, ROM_ZoneName).Contains(Room))
                .GroupBy(element => GetParameterValue(element.Parameters, LevelNameEng));

            using (var t = new Transaction(doc, TransactionName))
            {
                try
                {
                    t.Start();
                    foreach (var roomGroup in roomGroupsByLevel)
                    {
                        var blockGroups = roomGroup.GroupBy(element => GetParameterValue(element.Parameters, BS_BlockName));
                        foreach (var blockGroup in blockGroups)
                        {
                            var subZoneGroups = blockGroup.GroupBy(element => GetParameterValue(element.Parameters, ROM_SubZoneName));
                            foreach (var subZoneGroup in subZoneGroups)
                            {
                                var zoneGroups = subZoneGroup.GroupBy(element => GetParameterValue(element.Parameters, ROM_ZoneName))
                                    .Select(group => new RoomProjection(group.Key, group))
                                    .Where(proj => !proj.UnknownRoom)
                                    .OrderBy(zoneGroup => zoneGroup.FlatNumber);

                                RoomProjection previous = null;
                                foreach (var current in zoneGroups)
                                {
                                    if (previous != null && Math.Abs(previous.FlatNumber - current.FlatNumber) == 1)
                                    {
                                        PaintRoom(previous.Elements);
                                    }
                                    previous = current;
                                }
                            }
                        }
                    }
                    t.Commit();
                }
                catch (Exception exc)
                {
                    TaskDialog.Show(ErrorBoxTitle, exc.Message);
                    t.RollBack();
                    return Result.Failed;
                }
            }

            return Result.Succeeded;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Возвращает строковое значение параметра по имени в указанном множестве параметров.
        /// </summary>
        private string GetParameterValue(ParameterSet paramters, string name)
        {
            foreach (var parameter in paramters)
            {
                if (parameter is Parameter param && param.Definition.Name == name)
                {
                    return param.AsString();
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Устанавливает строковое значение параметра по имени в указанном множестве параметров.
        /// </summary>
        private void SetParameterValue(ParameterSet paramters, string name, string value)
        {
            foreach (var parameter in paramters)
            {
                if (parameter is Parameter param && param.Definition.Name == name)
                {
                    param.Set(value);
                }
            }
        }

        /// <summary>
        /// Закрашивает указанную коллекцию элементов в Полутон.
        /// </summary>
        /// <param name="elements">Указанная коллекция элементов.</param>
        private void PaintRoom(IEnumerable<Element> elements)
        {
            foreach (var element in elements)
            {
                var value = GetParameterValue(element.Parameters, ROM_Calc_SubZoneIdName) + ColorSuffix;
                SetParameterValue(element.Parameters, ROM_SubZoneIndexName, value);
            }
        }

#if DEBUG
        /// <summary>
        /// Используется для Отладки. Показывает окно со списком элементов.
        /// </summary>
        /// <param name="elements">Указанная коллекция элементов.</param>
        private void ShowElements(IEnumerable<Element> elements)
        {
            var sb = new StringBuilder();
            foreach (var element in elements)
            {
                sb.Append($"{GetParameterValue(element.Parameters, "Name")},");
            }
            TaskDialog.Show("Debug", sb.ToString());
        }
#endif

        #endregion
    }
}