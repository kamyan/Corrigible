using System.Runtime.CompilerServices;

// EditMode testleri Core'un iç (internal) üyelerini görür: ör. üretilmiş haritanın geçerlilik kurallarını bozulmuş bir kopyada
// sınamak (WorldMapTests). Oyun kodu iç üyelere dayanmaz.
[assembly: InternalsVisibleTo("Corrigible.Tests.EditMode")]
