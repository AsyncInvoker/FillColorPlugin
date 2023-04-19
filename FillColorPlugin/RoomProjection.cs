using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace FillColorPlugin
{
    /// <summary>
    /// Проекция для помещения.
    /// </summary>
    internal class RoomProjection
    {
        #region ctor

        /// <summary>
        /// Создаёт новый объект <see cref="RoomProjection"/> из указанного ключа и коллекции элементов.
        /// </summary>
        public RoomProjection(string key, IEnumerable<Element> elements)
        {
            var index = key.Length - 2;
            if (index > 0 && int.TryParse(key.Substring(index), out var result))
            {
                UnknownRoom = false;
                FlatNumber = result;
            }
            else
            {
                UnknownRoom = true;
            }

            Elements = elements;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Ключ данной проекции (например квартира 01 итд).
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Возвращает номер квартиры (число).
        /// </summary>
        public int FlatNumber { get; private set; }

        /// <summary>
        /// Признак того что не удалось распознать номер квартиры (часло) <see cref="FlatNumber"/>.
        /// </summary>
        public bool UnknownRoom { get; private set; }

        /// <summary>
        /// Возвращает коллекцию элементов для данной проекции.
        /// </summary>
        public IEnumerable<Element> Elements { get; private set; }

        #endregion
    }
}
