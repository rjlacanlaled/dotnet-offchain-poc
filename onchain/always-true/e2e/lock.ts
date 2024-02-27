import * as cbor from "https://deno.land/x/cbor@v1.4.1/index.js";
import {
  Blockfrost,
  Constr,
  Data,
  fromHex,
  Lucid,
  SpendingValidator,
  toHex,
  TxHash,
} from "https://deno.land/x/lucid@0.10.7/mod.ts";

const lucid = await Lucid.new(
  new Blockfrost(
    "https://cardano-preview.blockfrost.io/api/v0",
    "previewSzjI8VuP5fLfLNTANHAXdz4iF3Jrkf0A",
  ),
  "Preview",
);

lucid.selectWalletFromSeed(
  "wink physical know way mix unable often muffin flash grape arrive supply cloud tool grain erase friend gaze family chase bachelor someone cradle inmate",
  {
    accountIndex: 0,
  },
);

const validator = await readValidator();

// --- Supporting functions

async function readValidator(): Promise<SpendingValidator> {
  const validator =
    JSON.parse(await Deno.readTextFile("../plutus.json")).validators[0];
  return {
    type: "PlutusV2",
    script: toHex(cbor.encode(fromHex(validator.compiledCode))),
  };
}

const datum = Data.to(new Constr(0, [42n]));

const txHash = await lock(5000000n, { into: validator, datum });

await lucid.awaitTx(txHash);

console.log(`1 tADA locked into the contract at:
      Tx ID: ${txHash}
      Datum: ${datum}
  `);

// --- Supporting functions

async function lock(
  lovelace: bigint,
  { into, datum }: { into: SpendingValidator; datum: string },
): Promise<TxHash> {
  const contractAddress = lucid.utils.validatorToAddress(into);

  const tx = await lucid
    .newTx()
    .payToContract(contractAddress, { inline: datum }, { lovelace })
    .complete();

  const signedTx = await tx.sign().complete();

  return signedTx.submit();
}
