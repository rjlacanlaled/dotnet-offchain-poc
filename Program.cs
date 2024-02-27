using CardanoSharp.Wallet;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Keys;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;

const string SEED_PHRASE = "wink physical know way mix unable often muffin flash grape arrive supply cloud tool grain erase friend gaze family chase bachelor someone cradle inmate";
const string COMPILED_AIKEN_CBOR = "588e010000323232323232323222232325333008323370e6eb4c004c01c018dd698009803802918068008a4c26cac64a66601066e1d200000113232533300d300f002149858dd6980680098030020b180300199299980399b87480000044c8c94ccc030c03800852616375a6018002600a0082c600a0064600a6ea80048c00cdd5000ab9a5573aaae7955cfaba157441";
var script = PlutusV2ScriptBuilder.Create
        .SetScript(COMPILED_AIKEN_CBOR.HexToByteArray())
        .Build();
// Restore a Mnemonic
var mnemonic = new MnemonicService().Restore(SEED_PHRASE);
// Fluent derivation API
PrivateKey rootKey = mnemonic.GetRootKey();

// This path will give us our Payment Key on index 0
string paymentPath = $"m/1852'/1815'/0'/0/0";
// The paymentPrv is Private Key of the specified path.
PrivateKey paymentPrv = rootKey.Derive(paymentPath);
// Get the Public Key from the Private Key
PublicKey paymentPub = paymentPrv.GetPublicKey(false);

// This path will give us our Stake Key on index 0
string stakePath = $"m/1852'/1815'/0'/2/0";
// The stakePrv is Private Key of the specified path
PrivateKey stakePrv = rootKey.Derive(stakePath);
// Get the Public Key from the Stake Private Key
PublicKey stakePub = stakePrv.GetPublicKey(false);

PlutusDataConstr constr = new()
{
    Alternative = 0,
    Value = new PlutusDataArray { Value = [new PlutusDataInt { Value = 42 }] }
};
DatumOption datum = new() { Data = constr };


TransactionInput lockedInput = new()
{
    TransactionId = "9e5f5493c3b46524642e3c1db59abd1ebfb648422f671c1bfffbd0dcafdc1c6e".HexToByteArray(),
    TransactionIndex = 0,
    Output = new()
    {
        Address = new Address("addr_test1wrpk2xx4pkm8ywavtl6933qr3zzkarrtls9lh94f7jw4y8qws8htz").GetBytes(),
        Value = new TransactionOutputValue { Coin = 5 * CardanoUtility.adaToLovelace },
        DatumOption = datum
    }
};

TransactionInput referenceInputUtxo = new()
{
    TransactionId = "6ca6ef223972616f49a9f85f2936aa6f2bc4fd2efc7c20a98a68e6212359f6f1".HexToByteArray(),
    TransactionIndex = 2,
    Output = new TransactionOutput()
    {
        Address = new Address("addr_test1qpn3syu4qcfl7dnlazll4mtqxpgzdd75luquc5372x3fzrgjafkzwy703nlh95k9qn569kjz2uxvpzustkhkaluad6sqy0jxum").GetBytes(),
        Value = new TransactionOutputValue { Coin = 17996815579 }
    }
};

// Collateral Input
TransactionInput collateralInput = new()
{
    TransactionId = "6f806aac16152617ff0f1fe287fd22f37a7f04c8a55c0664649238999589a373".HexToByteArray(),
    TransactionIndex = 1,
    Output = new TransactionOutput()
    {
        Address = new Address("addr_test1qpn3syu4qcfl7dnlazll4mtqxpgzdd75luquc5372x3fzrgjafkzwy703nlh95k9qn569kjz2uxvpzustkhkaluad6sqy0jxum").GetBytes(),
        Value = new TransactionOutputValue { Coin = 900961578 }
    }
};

var payment1Addr = "addr_test1vrgvgwfx4xyu3r2sf8nphh4l92y84jsslg5yhyr8xul29rczf3alu".ToAddress();

// UTXO that has reference to the script
TransactionInput referenceScriptUtxo = new()
{
    TransactionId = "21e50398c351a120f6a9a73a1dda12970943d2814a045fa8aa9ce475e2309761".HexToByteArray(),
    TransactionIndex = 0,
    Output = new TransactionOutput()
    {
        Address = new Address("addr_test1vp7yptd2khhc0jf2vspj40ul6kgkff3wx7hdrhuqejjlfzquzf422").GetBytes(),
        Value = new TransactionOutputValue { Coin = 17996644798 },
        ScriptReference = new ScriptReference()
        {
            PlutusV2Script = script
        }
    }
};

Unlock(lockedInput, payment1Addr);
// CreateReferenceScript(referenceInputUtxo, script);

void Unlock(TransactionInput lockedInput, Address outputAddress)
{
    ulong fee = 0;
    var outputAmount = lockedInput!.Output!.Value.Coin - fee;

    // Redeemer
    RedeemerBuilder spendRedeemderBuilder = (RedeemerBuilder)RedeemerBuilder.Create
        .SetTag(RedeemerTag.Spend)
        .SetIndex(0)
        .SetPlutusData(new PlutusDataConstr
        {
            Alternative = 0,
            Value = new PlutusDataArray
            {
                Value = [new PlutusDataInt { Value = 42 }]
            }
        })
        .SetExUnits(new ExUnits { Mem = 1122686, Steps = 345104086 });

    var txBody = TransactionBodyBuilder.Create
        .AddInput(lockedInput)
        .AddOutput(outputAddress.GetBytes(), outputAmount)
        .SetFee(fee)
        .SetScriptDataHash(
            [spendRedeemderBuilder.Build()],
            null,
            CostModelUtility.PlutusV2CostModel.Serialize()
        )
        .AddCollateralInput(collateralInput)
        .AddReferenceInput(referenceScriptUtxo);

    //witnesses
    var witnesses = TransactionWitnessSetBuilder.Create
        .AddVKeyWitness(
        new PublicKey(paymentPub.Key, paymentPub.Chaincode),
        new PrivateKey(paymentPrv.Key, paymentPrv.Chaincode))
        .AddRedeemer(spendRedeemderBuilder);

    var tx = TransactionBuilder.Create
         .SetBody(txBody)
         .SetWitnesses(witnesses)
         .Build();

    var cborTransaction = tx.Serialize().ToString();
    var actualFee = tx.CalculateAndSetFee();

    Console.WriteLine($"Actual Fee: {actualFee}");
    Console.WriteLine($"Serialized: {cborTransaction}");
    File.WriteAllText("tx.raw", cborTransaction);
}

void CreateReferenceScript(TransactionInput refInputUtxo, PlutusV2Script script)
{
    string utxoAddress = "addr_test1vp7yptd2khhc0jf2vspj40ul6kgkff3wx7hdrhuqejjlfzquzf422";
    ulong fee = 0;
    var output = refInputUtxo!.Output!.Value.Coin - fee;

    var txBody = TransactionBodyBuilder.Create
            //change and fee dictated by control
            .SetFee(fee)
            .AddInput(refInputUtxo)
            .AddOutput(
                utxoAddress.ToAddress().GetBytes(),
                output,
                scriptReference: new ScriptReference()
                {
                    PlutusV2Script = script
                }
        );

    var witness = TransactionWitnessSetBuilder.Create.AddVKeyWitness(
    new PublicKey(paymentPub.Key, paymentPub.Chaincode),
    new PrivateKey(paymentPrv.Key, paymentPrv.Chaincode));

    var tx = TransactionBuilder.Create
        .SetBody(txBody)
        .SetWitnesses(witness)
        .Build();
    var cborTransaction = tx.Serialize().ToStringHex();
    var actualFee = tx.CalculateAndSetFee();

    Console.WriteLine($"Actual Fee: {actualFee}");
    Console.WriteLine($"Serialized: {cborTransaction}");
    File.WriteAllText("tx.raw", cborTransaction);
}
